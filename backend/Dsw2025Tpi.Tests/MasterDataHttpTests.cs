using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Dsw2025Tpi.Api.Authorization;
using Dsw2025Tpi.Api.Controllers;
using Dsw2025Tpi.Api.Services;
using Dsw2025Tpi.Application.MasterData;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dsw2025Tpi.Tests;

public sealed class MasterDataHttpTests
{
    private static readonly Guid IngenioA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid IngenioB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid DriverAId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001");
    private static readonly Guid DriverBId = Guid.Parse("bbbbbbbb-0000-4000-8000-000000000001");

    [Theory]
    [InlineData("GET", "/api/v1/drivers")]
    [InlineData("POST", "/api/v1/drivers")]
    public async Task Protected_catalog_routes_require_authentication(string method, string path)
    {
        using var app = new CatalogTestServer();
        using var request = Request(method, path, body: "{\"name\":\"No Auth\",\"dni\":\"DNI 900\",\"whatsApp\":\"+54 11 900\"}");

        using var response = await app.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain(app.Store.Transportistas, driver => driver.Name == "No Auth");
    }

    [Fact]
    public async Task Authenticated_role_without_catalog_permission_is_forbidden()
    {
        using var app = new CatalogTestServer();
        using var request = Request("POST", "/api/v1/drivers", "gerente-a",
            "{\"name\":\"Forbidden\",\"dni\":\"DNI 901\",\"whatsApp\":\"+54 11 901\"}");

        using var response = await app.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(app.Store.Transportistas, driver => driver.Name == "Forbidden");
    }

    [Theory]
    [InlineData("operador-a", "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa")]
    [InlineData("supervisor-a", "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa")]
    public async Task Approved_roles_can_list_create_edit_and_inactivate_within_their_tenant(string identity, string tenantId)
    {
        var tenant = Guid.Parse(tenantId);
        using var app = new CatalogTestServer();
        var seeded = app.Store.Transportistas.Single(driver => driver.Id == DriverAId);

        using var listResponse = await app.Client.SendAsync(Request("GET", "/api/v1/drivers", identity));
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listed = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Driver A", listed);
        Assert.DoesNotContain("Driver B private", listed);

        using var createRequest = Request("POST", "/api/v1/drivers", identity,
            "{\"name\":\"Created in A\",\"dni\":\"DNI 910\",\"whatsApp\":\"+54 11 910\"}");
        using var createResponse = await app.Client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var receipt = await createResponse.Content.ReadFromJsonAsync<MutationReceipt>();
        Assert.NotNull(receipt);
        var created = app.Store.Transportistas.Single(driver => driver.Id == receipt.ResourceId);
        Assert.Equal(tenant, created.IngenioId);

        using var updateRequest = Request("PUT", $"/api/v1/drivers/{DriverAId:D}", identity,
            "{\"name\":\"Updated in A\",\"dni\":\"DNI 911\",\"whatsApp\":\"+54 11 911\"}");
        using var updateResponse = await app.Client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Updated in A", seeded.Name);
        Assert.Equal(IngenioA, seeded.IngenioId);

        using var inactivateRequest = Request("POST", $"/api/v1/drivers/{DriverAId:D}/inactivation", identity);
        using var inactivateResponse = await app.Client.SendAsync(inactivateRequest);
        Assert.Equal(HttpStatusCode.OK, inactivateResponse.StatusCode);
        Assert.False(seeded.IsActive);
        Assert.Equal(IngenioA, seeded.IngenioId);
        Assert.DoesNotContain(app.Store.Transportistas, driver => driver.Name == "Updated in A" && driver.Id != DriverAId);
    }

    [Theory]
    [InlineData("operador-a", "bbbbbbbb-0000-4000-8000-000000000001", "Driver B private")]
    [InlineData("supervisor-a", "bbbbbbbb-0000-4000-8000-000000000001", "Driver B private")]
    [InlineData("operador-b", "aaaaaaaa-0000-4000-8000-000000000001", "Driver A")]
    [InlineData("supervisor-b", "aaaaaaaa-0000-4000-8000-000000000001", "Driver A")]
    public async Task Tenant_cannot_read_or_mutate_another_tenants_driver(string identity, string foreignDriver, string privateName)
    {
        var foreignDriverId = Guid.Parse(foreignDriver);
        using var app = new CatalogTestServer();

        using var getResponse = await app.Client.SendAsync(Request("GET", $"/api/v1/drivers/{foreignDriverId:D}", identity));
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.DoesNotContain(privateName, await getResponse.Content.ReadAsStringAsync());

        using var updateRequest = Request("PUT", $"/api/v1/drivers/{foreignDriverId:D}", identity,
            "{\"name\":\"Cross-tenant mutation\",\"dni\":\"DNI 920\",\"whatsApp\":\"+54 11 920\"}");
        using var updateResponse = await app.Client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.DoesNotContain(privateName, await updateResponse.Content.ReadAsStringAsync());
        Assert.DoesNotContain(app.Store.Transportistas, driver => driver.Name == "Cross-tenant mutation");
        Assert.Equal("Driver A", app.Store.Transportistas.Single(driver => driver.Id == DriverAId).Name);
        Assert.Equal("Driver B private", app.Store.Transportistas.Single(driver => driver.Id == DriverBId).Name);
    }

    [Fact]
    public async Task Forged_request_tenant_does_not_override_authenticated_tenant()
    {
        using var app = new CatalogTestServer();
        using var request = Request("POST", "/api/v1/drivers", "operador-a",
            "{\"name\":\"Forged tenant payload\",\"dni\":\"DNI 930\",\"whatsApp\":\"+54 11 930\",\"ingenioId\":\"bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb\"}");

        using var response = await app.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = Assert.Single(app.Store.Transportistas, driver => driver.Name == "Forged tenant payload");
        Assert.Equal(IngenioA, created.IngenioId);
        Assert.DoesNotContain(app.Store.Transportistas, driver => driver.IngenioId == IngenioB && driver.Name == "Forged tenant payload");
    }

    [Theory]
    [InlineData("operador-a", "POST", "/api/v1/drivers/aaaaaaaa-0000-4000-8000-000000000001/reactivation")]
    [InlineData("operador-a", "POST", "/api/v1/trucks/aaaaaaaa-0000-4000-8000-000000000001/reactivation")]
    [InlineData("operador-a", "POST", "/api/v1/farms/aaaaaaaa-0000-4000-8000-000000000001/reactivation")]
    [InlineData("operador-a", "GET", "/api/v1/driver-truck-associations")]
    [InlineData("operador-a", "POST", "/api/v1/driver-truck-associations")]
    [InlineData("operador-a", "GET", "/api/v1/driver-truck-associations/aaaaaaaa-0000-4000-8000-000000000001")]
    [InlineData("operador-a", "POST", "/api/v1/driver-truck-associations/aaaaaaaa-0000-4000-8000-000000000001/inactivation")]
    [InlineData("operador-a", "POST", "/api/v1/driver-truck-associations/aaaaaaaa-0000-4000-8000-000000000001/reactivation")]
    [InlineData("supervisor-a", "POST", "/api/v1/drivers/aaaaaaaa-0000-4000-8000-000000000001/reactivation")]
    [InlineData("supervisor-a", "POST", "/api/v1/trucks/aaaaaaaa-0000-4000-8000-000000000001/reactivation")]
    [InlineData("supervisor-a", "POST", "/api/v1/farms/aaaaaaaa-0000-4000-8000-000000000001/reactivation")]
    [InlineData("supervisor-a", "GET", "/api/v1/driver-truck-associations")]
    [InlineData("supervisor-a", "POST", "/api/v1/driver-truck-associations")]
    [InlineData("supervisor-a", "GET", "/api/v1/driver-truck-associations/aaaaaaaa-0000-4000-8000-000000000001")]
    [InlineData("supervisor-a", "POST", "/api/v1/driver-truck-associations/aaaaaaaa-0000-4000-8000-000000000001/inactivation")]
    [InlineData("supervisor-a", "POST", "/api/v1/driver-truck-associations/aaaaaaaa-0000-4000-8000-000000000001/reactivation")]
    public async Task Withdrawn_routes_return_not_found_for_approved_catalog_roles(string identity, string method, string path)
    {
        using var app = new CatalogTestServer();
        using var request = Request(method, path, identity,
            method == "POST" && path.EndsWith("driver-truck-associations", StringComparison.Ordinal)
                ? "{\"driverId\":\"aaaaaaaa-0000-4000-8000-000000000001\",\"truckId\":\"aaaaaaaa-0000-4000-8000-000000000002\"}"
                : null);

        using var response = await app.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Public_registration_route_is_not_exposed()
    {
        using var app = new CatalogTestServer();
        using var request = Request("POST", "/api/auth/register", body: "{}");

        using var response = await app.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static HttpRequestMessage Request(string method, string path, string? identity = null, string? body = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (identity is not null) request.Headers.Add(TestAuthenticationHandler.HeaderName, identity);
        if (body is not null) request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        if (method is "POST" or "PUT") request.Headers.Add("Idempotency-Key", $"http-test-{Guid.NewGuid():N}");
        return request;
    }

    private sealed class CatalogTestServer : IDisposable
    {
        private readonly TestServer _server;

        public CatalogTestServer()
        {
            Store = new MasterDataServiceTests.FakeMasterDataUnitOfWork();
            var driverA = new Transportista(IngenioA, "Driver A", "DNI 101", "+54 11 101") { Id = DriverAId };
            var driverB = new Transportista(IngenioB, "Driver B private", "DNI 201", "+54 11 201") { Id = DriverBId };
            Store.Add(driverA);
            Store.Add(driverB);

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IMasterDataUnitOfWork>(Store);
                    services.AddHttpContextAccessor();
                    services.AddScoped<ITenantContext, HttpTenantContext>();
                    services.AddScoped<MasterDataService>();
                    services.AddAuthentication(TestAuthenticationHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
                    services.AddAgroFlowAuthorizationPolicies();
                    services.AddControllers().AddApplicationPart(typeof(MasterDataController).Assembly);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            _server = new TestServer(builder);
            Client = _server.CreateClient();
        }

        public HttpClient Client { get; }
        public MasterDataServiceTests.FakeMasterDataUnitOfWork Store { get; }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "TestOnly";
        public const string HeaderName = "X-AgroFlow-Test-Principal";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var header)) return Task.FromResult(AuthenticateResult.NoResult());
            var principal = header.ToString() switch
            {
                "operador-a" => Principal("user-a-operator", IngenioA, AgroFlowRoles.Operador),
                "supervisor-a" => Principal("user-a-supervisor", IngenioA, AgroFlowRoles.Supervisor),
                "operador-b" => Principal("user-b-operator", IngenioB, AgroFlowRoles.Operador),
                "supervisor-b" => Principal("user-b-supervisor", IngenioB, AgroFlowRoles.Supervisor),
                "gerente-a" => Principal("user-a-unapproved-role", IngenioA, AgroFlowRoles.Gerente),
                _ => null
            };
            return Task.FromResult(principal is null
                ? AuthenticateResult.Fail("Unknown test principal.")
                : AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
        }

        private static ClaimsPrincipal Principal(string subject, Guid tenant, string role) => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim("sub", subject),
            new Claim(JwtTokenService.IngenioIdClaimType, tenant.ToString("D")),
            new Claim(ClaimTypes.Role, role)
        ], SchemeName));
    }
}
