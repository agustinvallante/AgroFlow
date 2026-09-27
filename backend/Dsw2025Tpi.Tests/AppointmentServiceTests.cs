using Dsw2025Tpi.Application.Appointments;
using Xunit;

namespace Dsw2025Tpi.Tests;

/// <summary>
/// Exercises Persona 3 orchestration against a fake port. These tests do not prove
/// real transactions, seed reproducibility or cross-process capacity; Persona 2's
/// persistence integration tests must cover those properties.
/// </summary>
public sealed class AppointmentServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-28T12:00:00+00:00");
    private static readonly CreateAppointmentCommand ValidCreate = new(
        "+5493815550101", "AF123BC", "FINCA-NORTE",
        DateTimeOffset.Parse("2026-09-28T05:30:00-03:00"), 28.5);

    [Fact]
    public async Task Create_selects_the_first_future_window_that_has_capacity()
    {
        var past = Candidate(-1, 2, 0);
        var full = Candidate(1, 2, 2);
        var firstAvailable = Candidate(2, 2, 1);
        var later = Candidate(3, 2, 0);
        var store = new FakeStore
        {
            CreationContext = new AppointmentCreationContext(false,
                [later, full, firstAvailable, past])
        };
        var service = new AppointmentService(store, new FixedClock(Now));

        var result = await service.CreateAsync(ValidCreate);

        Assert.Equal(AppointmentStatus.Asignado, result.Status);
        Assert.Equal([firstAvailable.Id], store.AttemptedWindowIds);
        Assert.Equal(firstAvailable.StartAt, result.Window.StartAt);
    }

    [Fact]
    public async Task Create_retries_the_next_window_after_a_concurrent_capacity_change()
    {
        var first = Candidate(1, 2, 1);
        var second = Candidate(2, 2, 0);
        var store = new FakeStore
        {
            CreationContext = new AppointmentCreationContext(false, [second, first])
        };
        store.CreateOutcomes.Enqueue(AppointmentCreateOutcome.CapacityChanged);
        var service = new AppointmentService(store, new FixedClock(Now));

        var result = await service.CreateAsync(ValidCreate);

        Assert.Equal([first.Id, second.Id], store.AttemptedWindowIds);
        Assert.Equal(second.StartAt, result.Window.StartAt);
    }

    [Fact]
    public async Task Create_rejects_missing_reference_active_truck_and_exhausted_windows()
    {
        var missingStore = new FakeStore { CreationContext = null };
        var activeStore = new FakeStore
        {
            CreationContext = new AppointmentCreationContext(true, [Candidate(1, 2, 0)])
        };
        var fullStore = new FakeStore
        {
            CreationContext = new AppointmentCreationContext(false,
                [Candidate(-1, 2, 0), Candidate(1, 2, 2)])
        };

        await AssertCodeAsync(AppointmentErrorCodes.ReferenceNotFound,
            () => new AppointmentService(missingStore, new FixedClock(Now)).CreateAsync(ValidCreate));
        await AssertCodeAsync(AppointmentErrorCodes.ActiveAppointmentExists,
            () => new AppointmentService(activeStore, new FixedClock(Now)).CreateAsync(ValidCreate));
        await AssertCodeAsync(AppointmentErrorCodes.NoCapacity,
            () => new AppointmentService(fullStore, new FixedClock(Now)).CreateAsync(ValidCreate));
        Assert.Empty(missingStore.AttemptedWindowIds);
        Assert.Empty(activeStore.AttemptedWindowIds);
        Assert.Empty(fullStore.AttemptedWindowIds);
    }

    [Fact]
    public async Task Create_rejects_invalid_input_before_accessing_the_store()
    {
        var store = new FakeStore();
        var service = new AppointmentService(store, new FixedClock(Now));
        var invalid = ValidCreate with { CarrierPhone = "invalid", EstimatedLoadTons = 0 };

        var error = await Assert.ThrowsAsync<AppointmentServiceException>(
            () => service.CreateAsync(invalid));

        Assert.Equal(AppointmentErrorCodes.ValidationError, error.Code);
        Assert.Contains("carrierPhone", error.Errors!.Keys);
        Assert.Contains("estimatedLoadTons", error.Errors.Keys);
        Assert.Equal(0, store.CreationContextReads);
    }

    [Theory]
    [InlineData(AppointmentCreateOutcome.ReferenceChanged, AppointmentErrorCodes.ReferenceNotFound)]
    [InlineData(AppointmentCreateOutcome.ActiveAppointmentExists, AppointmentErrorCodes.ActiveAppointmentExists)]
    [InlineData(AppointmentCreateOutcome.CapacityChanged, AppointmentErrorCodes.NoCapacity)]
    public async Task Create_maps_atomic_commit_conflicts_without_reporting_success(
        AppointmentCreateOutcome outcome, string expectedCode)
    {
        var store = new FakeStore();
        store.CreateOutcomes.Enqueue(outcome);
        var service = new AppointmentService(store, new FixedClock(Now));

        await AssertCodeAsync(expectedCode, () => service.CreateAsync(ValidCreate));

        Assert.Single(store.AttemptedWindowIds);
    }

    [Fact]
    public async Task List_forwards_filters_and_sorts_by_window_start()
    {
        var store = new FakeStore();
        var late = store.Current with
        {
            Id = Guid.NewGuid(),
            Window = new AppointmentWindow(Now.AddHours(3), Now.AddHours(3.5))
        };
        var early = store.Current with
        {
            Id = Guid.NewGuid(),
            Window = new AppointmentWindow(Now.AddHours(1), Now.AddHours(1.5))
        };
        store.Listed = [ToSummary(late), ToSummary(early)];
        var service = new AppointmentService(store, new FixedClock(Now));
        var query = new AppointmentQuery(new DateOnly(2026, 9, 28),
            AppointmentStatus.Asignado, "AF123BC", "+5493815550101");

        var result = await service.ListAsync(query);

        Assert.Same(query, store.LastQuery);
        Assert.Equal([early.Id, late.Id], result.Select(appointment => appointment.Id));
    }

    [Fact]
    public async Task Get_missing_appointment_uses_the_canonical_not_found_code()
    {
        var store = new FakeStore { Current = null! };
        var service = new AppointmentService(store);

        await AssertCodeAsync(AppointmentErrorCodes.AppointmentNotFound,
            () => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Transition_accepts_only_the_immediate_sequence_and_persists_each_step()
    {
        var store = new FakeStore();
        var service = new AppointmentService(store);
        var id = store.Current.Id;
        var sequence = new[]
        {
            AppointmentStatus.EnCamino,
            AppointmentStatus.EnEspera,
            AppointmentStatus.Ingresado,
            AppointmentStatus.EnDescarga,
            AppointmentStatus.Finalizado
        };

        await AssertCodeAsync(AppointmentErrorCodes.InvalidTransition,
            () => service.TransitionAsync(id, AppointmentStatus.Ingresado));
        Assert.Empty(store.TransitionCalls);

        foreach (var next in sequence)
        {
            var result = await service.TransitionAsync(id, next);
            Assert.Equal(next, result.Status);
            Assert.Equal(next, store.Current.Status);
        }

        Assert.Equal(sequence, store.TransitionCalls.Select(call => call.NextStatus));
        Assert.All(store.TransitionCalls, call => Assert.False(call.ReleaseCapacity));
        await AssertCodeAsync(AppointmentErrorCodes.InvalidTransition,
            () => service.TransitionAsync(id, AppointmentStatus.Cancelado));
    }

    [Theory]
    [InlineData(AppointmentStatus.Asignado)]
    [InlineData(AppointmentStatus.EnCamino)]
    [InlineData(AppointmentStatus.EnEspera)]
    public async Task Early_cancellation_requests_atomic_capacity_release(AppointmentStatus currentStatus)
    {
        var store = new FakeStore
        {
            Current = FakeStore.NewSnapshot(AppointmentStatus.Asignado) with { Status = currentStatus }
        };
        var service = new AppointmentService(store);

        var result = await service.TransitionAsync(store.Current.Id, AppointmentStatus.Cancelado);

        Assert.Equal(AppointmentStatus.Cancelado, result.Status);
        Assert.True(Assert.Single(store.TransitionCalls).ReleaseCapacity);
        Assert.Equal(AppointmentStatus.Cancelado, store.Current.Status);
        await AssertCodeAsync(AppointmentErrorCodes.InvalidTransition,
            () => service.TransitionAsync(store.Current.Id, AppointmentStatus.EnCamino));
    }

    [Theory]
    [InlineData(AppointmentStatus.Ingresado)]
    [InlineData(AppointmentStatus.EnDescarga)]
    [InlineData(AppointmentStatus.Finalizado)]
    [InlineData(AppointmentStatus.Cancelado)]
    public async Task Late_or_terminal_cancellation_does_not_mutate_the_store(AppointmentStatus currentStatus)
    {
        var store = new FakeStore
        {
            Current = FakeStore.NewSnapshot(currentStatus)
        };
        var service = new AppointmentService(store);

        await AssertCodeAsync(AppointmentErrorCodes.InvalidTransition,
            () => service.TransitionAsync(store.Current.Id, AppointmentStatus.Cancelado));

        Assert.Equal(currentStatus, store.Current.Status);
        Assert.Empty(store.TransitionCalls);
    }

    [Fact]
    public async Task Concurrent_state_change_is_reported_as_conflict()
    {
        var store = new FakeStore { ForcedTransitionOutcome = AppointmentTransitionOutcome.StateChanged };
        var service = new AppointmentService(store);

        await AssertCodeAsync(AppointmentErrorCodes.InvalidTransition,
            () => service.TransitionAsync(store.Current.Id, AppointmentStatus.EnCamino));
        Assert.Equal(AppointmentStatus.Asignado, store.Current.Status);
    }

    [Fact]
    public async Task Appointment_deleted_between_read_and_transition_is_reported_not_found()
    {
        var store = new FakeStore { ForcedTransitionOutcome = AppointmentTransitionOutcome.NotFound };
        var service = new AppointmentService(store);

        await AssertCodeAsync(AppointmentErrorCodes.AppointmentNotFound,
            () => service.TransitionAsync(store.Current.Id, AppointmentStatus.EnCamino));
    }

    private static AppointmentCandidateWindow Candidate(int hoursFromNow, int capacity, int occupied) =>
        new(Guid.NewGuid(), Now.AddHours(hoursFromNow), Now.AddHours(hoursFromNow).AddMinutes(30),
            capacity, occupied);

    private static AppointmentSummary ToSummary(AppointmentSnapshot snapshot) =>
        new(snapshot.Id, snapshot.Truck, snapshot.Window, snapshot.Status);

    private static async Task AssertCodeAsync(string code, Func<Task> operation)
    {
        var error = await Assert.ThrowsAsync<AppointmentServiceException>(operation);
        Assert.Equal(code, error.Code);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStore : IAppointmentStore
    {
        public AppointmentSnapshot Current { get; set; } = NewSnapshot(AppointmentStatus.Asignado);
        public AppointmentCreationContext? CreationContext { get; set; } =
            new(false, [Candidate(1, 2, 0)]);
        public int CreationContextReads { get; private set; }
        public Queue<AppointmentCreateOutcome> CreateOutcomes { get; } = new();
        public List<Guid> AttemptedWindowIds { get; } = [];
        public IReadOnlyList<AppointmentSummary> Listed { get; set; } = [];
        public AppointmentQuery? LastQuery { get; private set; }
        public List<(AppointmentStatus ExpectedStatus, AppointmentStatus NextStatus, bool ReleaseCapacity)>
            TransitionCalls { get; } = [];
        public AppointmentTransitionOutcome? ForcedTransitionOutcome { get; set; }

        public Task<AppointmentCreationContext?> GetCreationContextAsync(
            CreateAppointmentCommand command, CancellationToken cancellationToken = default)
        {
            CreationContextReads++;
            return Task.FromResult(CreationContext);
        }

        public Task<AppointmentCreateResult> TryCreateAssignedAsync(
            CreateAppointmentCommand command, Guid windowId, CancellationToken cancellationToken = default)
        {
            AttemptedWindowIds.Add(windowId);
            var outcome = CreateOutcomes.Count > 0 ? CreateOutcomes.Dequeue() : AppointmentCreateOutcome.Success;
            var window = CreationContext!.Windows.Single(candidate => candidate.Id == windowId);
            var created = Current with
            {
                Status = AppointmentStatus.Asignado,
                Window = new AppointmentWindow(window.StartAt, window.EndAt)
            };
            return Task.FromResult(new AppointmentCreateResult(
                outcome,
                outcome == AppointmentCreateOutcome.Success ? created : null));
        }

        public Task<IReadOnlyList<AppointmentSummary>> ListAsync(
            AppointmentQuery query, CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            return Task.FromResult(Listed);
        }

        public Task<AppointmentSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AppointmentSnapshot?>(Current is not null && Current.Id == id ? Current : null);

        public Task<AppointmentTransitionResult> TryTransitionAsync(
            Guid id, AppointmentStatus expectedStatus, AppointmentStatus nextStatus,
            bool releaseCapacity, CancellationToken cancellationToken = default)
        {
            TransitionCalls.Add((expectedStatus, nextStatus, releaseCapacity));
            var outcome = ForcedTransitionOutcome ??
                (Current.Status == expectedStatus
                    ? AppointmentTransitionOutcome.Success
                    : AppointmentTransitionOutcome.StateChanged);
            if (outcome == AppointmentTransitionOutcome.Success)
            {
                Current = Current with { Status = nextStatus };
            }
            return Task.FromResult(new AppointmentTransitionResult(
                outcome,
                outcome == AppointmentTransitionOutcome.Success ? Current : null));
        }

        public static AppointmentSnapshot NewSnapshot(AppointmentStatus status) => new(
            Guid.NewGuid(),
            new AppointmentReference(Guid.NewGuid(), "Transportista Demo"),
            new AppointmentTruckReference(Guid.NewGuid(), "AF123BC"),
            new AppointmentReference(Guid.NewGuid(), "FINCA-NORTE"),
            ValidCreate.CutAt,
            ValidCreate.EstimatedLoadTons,
            new AppointmentWindow(Now.AddHours(1), Now.AddHours(1.5)),
            status,
            Now);
    }
}
