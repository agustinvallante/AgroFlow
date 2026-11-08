using System.Net;
using AgroFlow.Api.Authorization;
using AgroFlow.Api.Controllers;
using AgroFlow.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgroFlow.Tests;

public sealed class MasterDataHttpTests
{
    private const string DriverId = "aaaaaaaa-0000-4000-8000-000000000001";

    // Las rutas de transportistas se publican sólo en LocalDemo, detrás de la
    // clave del dashboard (ver ConversationsHttpTests); fuera de ese perfil,
    // como aquí, tampoco existen.
    [Theory]
    [InlineData("GET", "/api/v1/drivers")]
    [InlineData("GET", "/api/v1/drivers/aaaaaaaa-0000-4000-8000-000000000001")]
    [InlineData("POST", "/api/v1/drivers")]
    [InlineData("PUT", "/api/v1/drivers/aaaaaaaa-0000-4000-8000-000000000001")]
    [InlineData("POST", "/api/v1/drivers/aaaaaaaa-0000-4000-8000-000000000001/inactivation")]
    [InlineData("POST", "/api/v1/drivers/aaaaaaaa-0000-4000-8000-000000000001/reactivation")]
    [InlineData("POST", "/api/v1/conversation-events")]
    [InlineData("GET", "/api/v1/conversations")]
    [InlineData("GET", "/api/v1/trucks")]
    [InlineData("GET", "/api/v1/farms")]
    [InlineData("GET", "/api/v1/driver-truck-associations")]
    [InlineData("POST", "/api/v1/driver-truck-associations")]
    [InlineData("GET", "/api/v1/driver-truck-associations/aaaaaaaa-0000-4000-8000-000000000001")]
    [InlineData("POST", "/api/v1/driver-truck-associations/aaaaaaaa-0000-4000-8000-000000000001/inactivation")]
    public async Task Preparatory_master_data_operations_are_not_exposed(string method, string path)
    {
        using var app = CreateTestServer();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT") request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        using var response = await app.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Preparatory_controller_retains_guarded_methods_but_is_excluded_from_mvc_discovery()
    {
        var type = typeof(MasterDataController);
        Assert.NotNull(type.GetCustomAttributes(typeof(NonControllerAttribute), inherit: true).SingleOrDefault());
        var operations = type.GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true).Cast<HttpMethodAttribute>()
                .SelectMany(attribute => attribute.HttpMethods.Select(verb => (method, verb, attribute.Template))))
            .Where(operation => operation.method.Name is not "ToString" and not "Equals" and not "GetHashCode" and not "GetType")
            .ToArray();
        Assert.Equal(15, operations.Length);
        Assert.All(operations, operation => Assert.NotNull(operation.method.GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault()));
        Assert.Contains(operations, operation => operation.method.Name == nameof(MasterDataController.CreateDriver)
            && operation.method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy == AgroFlowPolicies.CatalogManagement);
    }

    [Fact]
    public async Task Public_registration_route_is_not_exposed()
    {
        using var app = CreateTestServer();
        using var response = await app.CreateClient().PostAsync("/api/auth/register", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static TestServer CreateTestServer()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services => services
                .AddControllers(options => options.Conventions.Add(new LocalDemoAppointmentsConvention(isLocalDemo: false)))
                .AddApplicationPart(typeof(MasterDataController).Assembly))
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            });
        return new TestServer(builder);
    }
}
