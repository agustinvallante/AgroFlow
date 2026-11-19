using AgroFlow.Application.Appointments;
using Xunit;

namespace AgroFlow.Tests;

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
    public async Task Create_delegates_natural_keys_and_preserves_seed_timezone_in_response()
    {
        var localOffset = TimeSpan.FromHours(-3);
        var localStart = Now.AddHours(1).ToOffset(localOffset);
        var candidate = new AppointmentCandidateWindow(
            Guid.NewGuid(), localStart, localStart.AddMinutes(30), 2, 0);
        var store = new FakeStore
        {
            CreationContext = new AppointmentCreationContext(false, [candidate]),
            Current = FakeStore.NewSnapshot(AppointmentStatus.Asignado) with
            {
                CreatedAt = Now.ToOffset(localOffset)
            }
        };
        var command = ValidCreate with
        {
            TruckPlate = "af-123 bc",
            FarmCode = "finca-norte"
        };
        var service = new AppointmentService(store, new FixedClock(Now));

        var result = await service.CreateAsync(command);

        Assert.Same(command, store.LastCreationContextCommand);
        Assert.Same(command, store.LastCreateCommitCommand);
        Assert.Equal("+5493815550101", store.LastCreationContextCommand!.CarrierPhone);
        Assert.Equal("af-123 bc", store.LastCreationContextCommand.TruckPlate);
        Assert.Equal("finca-norte", store.LastCreationContextCommand.FarmCode);
        Assert.Equal("AF123BC", result.Truck.Plate);
        Assert.Equal(localOffset, result.CutAt.Offset);
        Assert.Equal(localOffset, result.CreatedAt.Offset);
        Assert.Equal(localOffset, result.Window.StartAt.Offset);
        Assert.Equal(localOffset, result.Window.EndAt.Offset);
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
    [InlineData("------")]
    [InlineData("--  --  --")]
    [InlineData("..////..")]
    public async Task Truck_plate_without_letters_or_digits_is_rejected_before_accessing_the_store(string plate)
    {
        var store = new FakeStore { Listed = [] };
        var service = new AppointmentService(store, new FixedClock(Now));

        var createError = await Assert.ThrowsAsync<AppointmentServiceException>(
            () => service.CreateAsync(ValidCreate with { TruckPlate = plate }));
        var listError = await Assert.ThrowsAsync<AppointmentServiceException>(
            () => service.ListAsync(new AppointmentQuery(null, null, plate, null)));

        foreach (var error in new[] { createError, listError })
        {
            Assert.Equal(AppointmentErrorCodes.ValidationError, error.Code);
            Assert.Equal(["truckPlate"], error.Errors!.Keys);
        }
        Assert.Equal(0, store.CreationContextReads);
        Assert.Null(store.LastQuery);
    }

    [Theory]
    [InlineData("AF123BC", true)]
    [InlineData("af-123 bc", true)]
    [InlineData("AB-12-34", true)]
    [InlineData("------", false)]
    [InlineData("- - - -", false)]
    [InlineData("AF1", false)]
    [InlineData("AF123BC12345", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Truck_plate_rule_requires_length_and_a_letter_or_digit(string? plate, bool expected) =>
        Assert.Equal(expected, AppointmentService.IsValidTruckPlate(plate));

    [Fact]
    public async Task Create_does_not_impose_an_undocumented_cutoff_on_cutAt()
    {
        var store = new FakeStore();
        var service = new AppointmentService(store, new FixedClock(Now));
        var command = ValidCreate with { CutAt = DateTimeOffset.MinValue };

        await service.CreateAsync(command);

        Assert.Equal(DateTimeOffset.MinValue, store.LastCreationContextCommand?.CutAt);
        Assert.Equal(DateTimeOffset.MinValue, store.LastCreateCommitCommand?.CutAt);
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
    public async Task List_without_date_preserves_other_filters_for_the_store()
    {
        var store = new FakeStore { Listed = [] };
        var service = new AppointmentService(store, new FixedClock(Now));
        var query = new AppointmentQuery(
            null, AppointmentStatus.EnCamino, "af-123 bc", "+5493815550101");

        var result = await service.ListAsync(query);

        // Persona 2 must interpret null as the seed ingenio's local today even
        // with these filters, and return an empty collection when none match.
        Assert.Same(query, store.LastQuery);
        Assert.Null(store.LastQuery!.Date);
        Assert.Equal(AppointmentStatus.EnCamino, store.LastQuery.Status);
        Assert.Equal("af-123 bc", store.LastQuery.TruckPlate);
        Assert.Equal("+5493815550101", store.LastQuery.Phone);
        Assert.Empty(result);
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
        // Only the step out of EN_CAMINO closes the open route incident.
        Assert.Equal([false, true, false, false, false], store.ResolveOpenIncidentCalls);
        await AssertCodeAsync(AppointmentErrorCodes.InvalidTransition,
            () => service.TransitionAsync(id, AppointmentStatus.Cancelado));
    }

    [Fact]
    public async Task Cancelling_en_camino_also_requests_resolving_the_open_incident()
    {
        var store = new FakeStore { Current = FakeStore.NewSnapshot(AppointmentStatus.EnCamino) };
        var service = new AppointmentService(store);

        await service.TransitionAsync(store.Current.Id, AppointmentStatus.Cancelado);

        Assert.Equal([true], store.ResolveOpenIncidentCalls);
    }

    [Theory]
    [InlineData(AppointmentStatus.Asignado)]
    [InlineData(AppointmentStatus.EnEspera)]
    [InlineData(AppointmentStatus.Finalizado)]
    public async Task Incident_report_outside_en_camino_is_rejected_before_the_store(AppointmentStatus status)
    {
        var store = new FakeStore { Current = FakeStore.NewSnapshot(status) };
        var service = new AppointmentService(store);

        await AssertCodeAsync(AppointmentErrorCodes.IncidentNotAllowed, () => service.ReportIncidentAsync(
            store.Current.Id, new ReportRouteIncidentCommand(RouteIncidentType.Pinchazo, true, 30, null)));

        Assert.Empty(store.ReportCalls);
    }

    [Theory]
    [InlineData((RouteIncidentType)99, 30, 0, "type")]
    [InlineData(RouteIncidentType.Otro, -1, 0, "estimatedDelayMinutes")]
    [InlineData(RouteIncidentType.Otro, 1441, 0, "estimatedDelayMinutes")]
    [InlineData(RouteIncidentType.Otro, 30, 501, "description")]
    public async Task Invalid_incident_report_is_a_field_validation_error(
        RouteIncidentType type, int delay, int descriptionLength, string field)
    {
        var store = new FakeStore { Current = FakeStore.NewSnapshot(AppointmentStatus.EnCamino) };
        var service = new AppointmentService(store);
        var description = descriptionLength == 0 ? null : new string('x', descriptionLength);

        var exception = await Assert.ThrowsAsync<AppointmentServiceException>(() => service.ReportIncidentAsync(
            store.Current.Id, new ReportRouteIncidentCommand(type, true, delay, description)));

        Assert.Equal(AppointmentErrorCodes.ValidationError, exception.Code);
        Assert.True(exception.Errors!.ContainsKey(field));
        Assert.Empty(store.ReportCalls);
    }

    [Fact]
    public async Task Incident_store_conflicts_map_to_contract_codes()
    {
        var store = new FakeStore { Current = FakeStore.NewSnapshot(AppointmentStatus.EnCamino) };
        var service = new AppointmentService(store);
        var command = new ReportRouteIncidentCommand(RouteIncidentType.Accidente, false, 60, "Choque leve");

        var reported = await service.ReportIncidentAsync(store.Current.Id, command);
        Assert.Equal(RouteIncidentType.Accidente, reported.OpenIncident!.Type);
        await AssertCodeAsync(AppointmentErrorCodes.OpenIncidentExists,
            () => service.ReportIncidentAsync(store.Current.Id, command));

        var resolved = await service.ResolveIncidentAsync(store.Current.Id);
        Assert.Null(resolved.OpenIncident);
        await AssertCodeAsync(AppointmentErrorCodes.NoOpenIncident,
            () => service.ResolveIncidentAsync(store.Current.Id));
        Assert.Equal(1, store.ResolveCalls);

        // A concurrent change detected by the store keeps its own code.
        store.IncidentOutcome = AppointmentIncidentOutcome.NotEnCamino;
        await AssertCodeAsync(AppointmentErrorCodes.IncidentNotAllowed,
            () => service.ReportIncidentAsync(store.Current.Id, command));
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
        public CreateAppointmentCommand? LastCreationContextCommand { get; private set; }
        public CreateAppointmentCommand? LastCreateCommitCommand { get; private set; }
        public Queue<AppointmentCreateOutcome> CreateOutcomes { get; } = new();
        public List<Guid> AttemptedWindowIds { get; } = [];
        public IReadOnlyList<AppointmentSummary> Listed { get; set; } = [];
        public AppointmentQuery? LastQuery { get; private set; }
        public List<(AppointmentStatus ExpectedStatus, AppointmentStatus NextStatus, bool ReleaseCapacity)>
            TransitionCalls { get; } = [];
        public AppointmentTransitionOutcome? ForcedTransitionOutcome { get; set; }
        public List<bool> ResolveOpenIncidentCalls { get; } = [];
        public List<ReportRouteIncidentCommand> ReportCalls { get; } = [];
        public int ResolveCalls { get; private set; }
        public AppointmentIncidentOutcome IncidentOutcome { get; set; } = AppointmentIncidentOutcome.Success;

        public Task<AppointmentIncidentResult> TryReportIncidentAsync(
            Guid id, ReportRouteIncidentCommand command, CancellationToken cancellationToken = default)
        {
            ReportCalls.Add(command);
            return Task.FromResult(IncidentResult(new RouteIncident(Guid.NewGuid(), command.Type, command.CanContinue,
                command.EstimatedDelayMinutes, command.Description, Now)));
        }

        public Task<AppointmentIncidentResult> TryResolveIncidentAsync(
            Guid id, CancellationToken cancellationToken = default)
        {
            ResolveCalls++;
            return Task.FromResult(IncidentResult(null));
        }

        private AppointmentIncidentResult IncidentResult(RouteIncident? incident)
        {
            if (IncidentOutcome != AppointmentIncidentOutcome.Success)
                return new AppointmentIncidentResult(IncidentOutcome, null);
            Current = Current with { OpenIncident = incident };
            return new AppointmentIncidentResult(IncidentOutcome, Current);
        }

        public Task<AppointmentCreationContext?> GetCreationContextAsync(
            CreateAppointmentCommand command, CancellationToken cancellationToken = default)
        {
            CreationContextReads++;
            LastCreationContextCommand = command;
            return Task.FromResult(CreationContext);
        }

        public Task<AppointmentCreateResult> TryCreateAssignedAsync(
            CreateAppointmentCommand command, Guid windowId, CancellationToken cancellationToken = default)
        {
            AttemptedWindowIds.Add(windowId);
            LastCreateCommitCommand = command;
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
            bool releaseCapacity, bool resolveOpenIncident, CancellationToken cancellationToken = default)
        {
            TransitionCalls.Add((expectedStatus, nextStatus, releaseCapacity));
            ResolveOpenIncidentCalls.Add(resolveOpenIncident);
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
