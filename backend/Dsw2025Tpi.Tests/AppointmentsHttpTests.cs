using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dsw2025Tpi.Api.Controllers;
using Dsw2025Tpi.Application.Appointments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Dsw2025Tpi.Tests;

/// <summary>
/// Tests the HTTP surface in isolation from the inherited SQL Server/Identity startup.
/// The fake service is not an alternative to Persona 2's persistent store.
/// </summary>
public sealed class AppointmentsHttpTests
{
    [Fact]
    public async Task Post_creates_an_assigned_appointment_with_location_and_canonical_fields()
    {
        var service = new FakeAppointmentService();
        using var app = await TestApp.StartAsync(service);

        using var response = await app.Client.PostAsJsonAsync("/api/v1/appointments", ValidCreateBody);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/v1/appointments/{service.Appointment.Id}", response.Headers.Location?.ToString());
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(service.Appointment.Id.ToString(), body.RootElement.GetProperty("id").GetString());
        Assert.Equal("ASIGNADO", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("AF123BC", body.RootElement.GetProperty("truck").GetProperty("plate").GetString());
        Assert.Equal(28.5, body.RootElement.GetProperty("estimatedLoadTons").GetDouble());
        Assert.EndsWith("-03:00", body.RootElement.GetProperty("cutAt").GetString());
        Assert.EndsWith("-03:00", body.RootElement.GetProperty("window").GetProperty("startAt").GetString());
        Assert.EndsWith("-03:00", body.RootElement.GetProperty("window").GetProperty("endAt").GetString());
        Assert.EndsWith("-03:00", body.RootElement.GetProperty("createdAt").GetString());
        Assert.NotNull(service.LastCreate);
        Assert.Equal("+5493815550101", service.LastCreate.CarrierPhone);
        Assert.Equal("FINCA-NORTE", service.LastCreate.FarmCode);
    }

    [Fact]
    public async Task Post_accepts_rfc3339_lowercase_date_time_separator_and_utc_marker()
    {
        var service = new FakeAppointmentService();
        using var app = await TestApp.StartAsync(service);
        var body = new
        {
            carrierPhone = "+5493815550101",
            truckPlate = "AF123BC",
            farmCode = "FINCA-NORTE",
            cutAt = "2026-09-28t08:30:00z",
            estimatedLoadTons = 28.5
        };

        using var response = await app.Client.PostAsJsonAsync("/api/v1/appointments", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(DateTimeOffset.Parse("2026-09-28T08:30:00Z"), service.LastCreate?.CutAt);
    }

    [Fact]
    public async Task Get_list_parses_combined_filters_and_returns_summaries()
    {
        var service = new FakeAppointmentService();
        using var app = await TestApp.StartAsync(service);

        using var response = await app.Client.GetAsync(
            "/api/v1/appointments?date=2026-09-28&status=EN_CAMINO&truckPlate=AF-123-BC&phone=%2B5493815550101");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new DateOnly(2026, 9, 28), service.LastQuery?.Date);
        Assert.Equal(AppointmentStatus.EnCamino, service.LastQuery?.Status);
        Assert.Equal("AF-123-BC", service.LastQuery?.TruckPlate);
        Assert.Equal("+5493815550101", service.LastQuery?.Phone);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var summary = Assert.Single(body.RootElement.EnumerateArray());
        Assert.Equal("ASIGNADO", summary.GetProperty("status").GetString());
        Assert.Equal("AF123BC", summary.GetProperty("truck").GetProperty("plate").GetString());
        Assert.False(summary.TryGetProperty("carrier", out _));
    }

    [Fact]
    public async Task Get_list_with_phone_but_no_date_leaves_local_today_to_the_store()
    {
        var service = new FakeAppointmentService();
        using var app = await TestApp.StartAsync(service);

        using var response = await app.Client.GetAsync(
            "/api/v1/appointments?phone=%2B5493815550101");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(service.LastQuery?.Date);
        Assert.Equal("+5493815550101", service.LastQuery?.Phone);
    }

    [Fact]
    public async Task Get_detail_returns_the_current_appointment()
    {
        var service = new FakeAppointmentService();
        using var app = await TestApp.StartAsync(service);

        using var response = await app.Client.GetAsync($"/api/v1/appointments/{service.Appointment.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(service.Appointment.Id, service.LastGetId);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("FINCA-NORTE", body.RootElement.GetProperty("farm").GetProperty("name").GetString());
        Assert.Equal("ASIGNADO", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Post_transition_returns_the_persisted_state()
    {
        var service = new FakeAppointmentService();
        using var app = await TestApp.StartAsync(service);

        using var response = await app.Client.PostAsJsonAsync(
            $"/api/v1/appointments/{service.Appointment.Id}/transitions",
            new { newStatus = "EN_CAMINO" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((service.Appointment.Id, AppointmentStatus.EnCamino), service.LastTransition);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("EN_CAMINO", body.RootElement.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("/api/v1/appointments?date=not-a-date")]
    [InlineData("/api/v1/appointments?status=ANYTHING")]
    [InlineData("/api/v1/appointments?phone=invalid")]
    [InlineData("/api/v1/appointments?phone=+5493815550101")]
    [InlineData("/api/v1/appointments?status=")]
    [InlineData("/api/v1/appointments?phone=%2B5493815550101&phone=%2B5493815550101")]
    [InlineData("/api/v1/appointments?cacheBust=123")]
    [InlineData("/api/v1/appointments/not-a-uuid")]
    public async Task Invalid_query_or_route_uses_canonical_validation_problem(string url)
    {
        using var app = await TestApp.StartAsync(new FakeAppointmentService());

        using var response = await app.Client.GetAsync(url);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, AppointmentErrorCodes.ValidationError);
    }

    [Theory]
    [InlineData("{\"carrierPhone\":\"bad\",\"truckPlate\":\"AF123BC\",\"farmCode\":\"FINCA-NORTE\",\"cutAt\":\"2026-09-28T05:30:00-03:00\",\"estimatedLoadTons\":28.5}")]
    [InlineData("{\"carrierPhone\":\"+5493815550101\",\"truckPlate\":\"AF123BC\",\"farmCode\":\"FINCA-NORTE\",\"cutAt\":\"2026-09-28T05:30:00-03:00\",\"estimatedLoadTons\":0}")]
    [InlineData("{\"carrierPhone\":\"+5493815550101\",\"truckPlate\":\"AF123BC\",\"farmCode\":\"FINCA-NORTE\",\"cutAt\":\"2026-09-28T05:30:00\",\"estimatedLoadTons\":28.5}")]
    [InlineData("{\"carrierPhone\":\"+5493815550101\",\"truckPlate\":\"AF123BC\",\"farmCode\":\"FINCA-NORTE\",\"cutAt\":\"2026-09-28T05:30:00-03:00\",\"estimatedLoadTons\":28.5,\"preferredWindow\":\"2026-09-28T08:00:00-03:00\"}")]
    [InlineData("{\"CarrierPhone\":\"+5493815550101\",\"truckPlate\":\"AF123BC\",\"farmCode\":\"FINCA-NORTE\",\"cutAt\":\"2026-09-28T05:30:00-03:00\",\"estimatedLoadTons\":28.5}")]
    [InlineData("{\"carrierPhone\":\"+5493815550101\",\"carrierPhone\":\"+5493815550101\",\"truckPlate\":\"AF123BC\",\"farmCode\":\"FINCA-NORTE\",\"cutAt\":\"2026-09-28T05:30:00-03:00\",\"estimatedLoadTons\":28.5}")]
    [InlineData("{\"carrierPhone\":42,\"truckPlate\":\"AF123BC\",\"farmCode\":\"FINCA-NORTE\",\"cutAt\":\"2026-09-28T05:30:00-03:00\",\"estimatedLoadTons\":28.5}")]
    [InlineData("{\"carrierPhone\":\"+5493815550101\",\"truckPlate\":\"AF123BC\",\"farmCode\":\"FINCA-NORTE\",\"cutAt\":\"2026-09-28T05:30:00-03:00\",\"estimatedLoadTons\":\"28.5\"}")]
    public async Task Invalid_create_body_reports_field_errors(string json)
    {
        using var app = await TestApp.StartAsync(new FakeAppointmentService());

        using var response = await app.Client.PostAsync("/api/v1/appointments",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        using var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest,
            AppointmentErrorCodes.ValidationError);
        Assert.Equal(JsonValueKind.Object, body.RootElement.GetProperty("errors").ValueKind);
    }

    [Theory]
    [InlineData("{\"carrierPhone\":")]
    [InlineData("not-json")]
    [InlineData("null")]
    public async Task Malformed_or_null_create_body_omits_field_errors(string json)
    {
        using var app = await TestApp.StartAsync(new FakeAppointmentService());

        using var response = await app.Client.PostAsync("/api/v1/appointments",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        using var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest,
            AppointmentErrorCodes.ValidationError);
        Assert.False(body.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Missing_create_body_omits_field_errors()
    {
        using var app = await TestApp.StartAsync(new FakeAppointmentService());

        using var response = await app.Client.PostAsync("/api/v1/appointments", null);

        using var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest,
            AppointmentErrorCodes.ValidationError);
        Assert.False(body.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Unsupported_content_type_uses_contractual_problem_details()
    {
        using var app = await TestApp.StartAsync(new FakeAppointmentService());

        using var response = await app.Client.PostAsync("/api/v1/appointments",
            new StringContent("{}", System.Text.Encoding.UTF8, "text/plain"));

        using var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest,
            AppointmentErrorCodes.ValidationError);
        Assert.False(body.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Missing_content_type_uses_contractual_problem_details()
    {
        using var app = await TestApp.StartAsync(new FakeAppointmentService());
        using var content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("{}"));

        using var response = await app.Client.PostAsync("/api/v1/appointments", content);

        using var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest,
            AppointmentErrorCodes.ValidationError);
        Assert.False(body.RootElement.TryGetProperty("errors", out _));
    }

    [Theory]
    [InlineData("{\"newStatus\":")]
    [InlineData("null")]
    public async Task Malformed_or_null_transition_body_omits_field_errors(string json)
    {
        var service = new FakeAppointmentService();
        using var app = await TestApp.StartAsync(service);

        using var response = await app.Client.PostAsync(
            $"/api/v1/appointments/{service.Appointment.Id}/transitions",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        using var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest,
            AppointmentErrorCodes.ValidationError);
        Assert.False(body.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Unknown_create_property_reports_its_name()
    {
        using var app = await TestApp.StartAsync(new FakeAppointmentService());
        const string json = "{\"preferredWindow\":\"2026-09-28T08:00:00-03:00\"}";

        using var response = await app.Client.PostAsync("/api/v1/appointments",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        using var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest,
            AppointmentErrorCodes.ValidationError);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("preferredWindow", out _));
    }

    [Fact]
    public async Task Unknown_transition_property_reports_its_name()
    {
        var service = new FakeAppointmentService();
        using var app = await TestApp.StartAsync(service);

        using var response = await app.Client.PostAsJsonAsync(
            $"/api/v1/appointments/{service.Appointment.Id}/transitions",
            new { newStatus = "EN_CAMINO", reason = "not in contract" });

        using var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest,
            AppointmentErrorCodes.ValidationError);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("reason", out _));
        Assert.Null(service.LastTransition);
    }

    [Fact]
    public async Task Missing_appointment_and_business_conflict_have_distinct_codes()
    {
        var service = new FakeAppointmentService
        {
            GetError = new AppointmentServiceException(AppointmentErrorCodes.AppointmentNotFound, "Not found"),
            TransitionError = new AppointmentServiceException(AppointmentErrorCodes.InvalidTransition, "Invalid transition")
        };
        using var app = await TestApp.StartAsync(service);

        using var missing = await app.Client.GetAsync($"/api/v1/appointments/{Guid.NewGuid()}");
        using var conflict = await app.Client.PostAsJsonAsync(
            $"/api/v1/appointments/{service.Appointment.Id}/transitions",
            new { newStatus = "FINALIZADO" });

        using var missingBody = await AssertProblemAsync(missing, HttpStatusCode.NotFound,
            AppointmentErrorCodes.AppointmentNotFound);
        using var conflictBody = await AssertProblemAsync(conflict, HttpStatusCode.Conflict,
            AppointmentErrorCodes.InvalidTransition);
    }

    [Theory]
    [InlineData("REFERENCE_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("ACTIVE_APPOINTMENT_EXISTS", HttpStatusCode.Conflict)]
    [InlineData("NO_CAPACITY", HttpStatusCode.Conflict)]
    public async Task Create_maps_store_failures_to_contractual_http_errors(string code, HttpStatusCode expected)
    {
        var service = new FakeAppointmentService
        {
            CreateError = new AppointmentServiceException(code, "Expected failure")
        };
        using var app = await TestApp.StartAsync(service);

        using var response = await app.Client.PostAsJsonAsync("/api/v1/appointments", ValidCreateBody);

        using var body = await AssertProblemAsync(response, expected, code);
    }

    [Fact]
    public async Task Unexpected_create_failure_is_sanitized_as_internal_error()
    {
        var service = new FakeAppointmentService
        {
            CreateError = new InvalidOperationException("internal database details must not leak")
        };
        using var app = await TestApp.StartAsync(service);

        using var response = await app.Client.PostAsJsonAsync("/api/v1/appointments", ValidCreateBody);

        using var body = await AssertProblemAsync(response, HttpStatusCode.InternalServerError,
            "INTERNAL_ERROR");
        Assert.DoesNotContain("internal database details", body.RootElement.GetRawText());
    }

    private static readonly object ValidCreateBody = new
    {
        carrierPhone = "+5493815550101",
        truckPlate = "AF123BC",
        farmCode = "FINCA-NORTE",
        cutAt = "2026-09-28T05:30:00-03:00",
        estimatedLoadTons = 28.5
    };

    private static async Task<JsonDocument> AssertProblemAsync(
        HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal((int)expectedStatus, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, body.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("type").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("title").GetString()));
        return body;
    }

    private sealed class TestApp : IDisposable
    {
        private readonly IHost _host;

        private TestApp(IHost host)
        {
            _host = host;
            Client = host.GetTestClient();
        }

        public HttpClient Client { get; }

        public static async Task<TestApp> StartAsync(FakeAppointmentService service)
        {
            var host = await new HostBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddControllers()
                            .AddApplicationPart(typeof(AppointmentsController).Assembly);
                        services.AddSingleton<IAppointmentService>(service);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    }))
                .StartAsync();
            return new TestApp(host);
        }

        public void Dispose()
        {
            Client.Dispose();
            _host.Dispose();
        }
    }

    private sealed class FakeAppointmentService : IAppointmentService
    {
        public AppointmentSnapshot Appointment { get; } = new(
            Guid.Parse("2337d4ae-09ac-45e6-8bba-379cd3e83c3f"),
            new AppointmentReference(Guid.NewGuid(), "Transportista Demo"),
            new AppointmentTruckReference(Guid.NewGuid(), "AF123BC"),
            new AppointmentReference(Guid.NewGuid(), "FINCA-NORTE"),
            DateTimeOffset.Parse("2026-09-28T05:30:00-03:00"),
            28.5,
            new AppointmentWindow(DateTimeOffset.Parse("2026-09-28T08:00:00-03:00"),
                DateTimeOffset.Parse("2026-09-28T08:30:00-03:00")),
            AppointmentStatus.Asignado,
            DateTimeOffset.Parse("2026-09-27T20:00:00-03:00"));

        public CreateAppointmentCommand? LastCreate { get; private set; }
        public AppointmentQuery? LastQuery { get; private set; }
        public Guid? LastGetId { get; private set; }
        public (Guid Id, AppointmentStatus Status)? LastTransition { get; private set; }
        public Exception? CreateError { get; set; }
        public AppointmentServiceException? GetError { get; set; }
        public AppointmentServiceException? TransitionError { get; set; }

        public Task<AppointmentSnapshot> CreateAsync(CreateAppointmentCommand command, CancellationToken cancellationToken = default)
        {
            LastCreate = command;
            if (CreateError is not null) throw CreateError;
            return Task.FromResult(Appointment);
        }

        public Task<IReadOnlyList<AppointmentSummary>> ListAsync(AppointmentQuery query, CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            IReadOnlyList<AppointmentSummary> appointments =
                [new AppointmentSummary(Appointment.Id, Appointment.Truck, Appointment.Window, Appointment.Status)];
            return Task.FromResult(appointments);
        }

        public Task<AppointmentSnapshot> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            LastGetId = id;
            if (GetError is not null) throw GetError;
            return Task.FromResult(Appointment);
        }

        public Task<AppointmentSnapshot> TransitionAsync(Guid id, AppointmentStatus newStatus, CancellationToken cancellationToken = default)
        {
            LastTransition = (id, newStatus);
            if (TransitionError is not null) throw TransitionError;
            return Task.FromResult(Appointment with { Status = newStatus });
        }
    }
}
