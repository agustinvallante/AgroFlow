using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AgroFlow.Api;
using AgroFlow.Api.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>
/// La aplicación real en LocalDemo: claves de servicio y de dashboard, contrato
/// estricto y recorrido completo del canal sobre SQLite migrado. Datos ficticios.
/// </summary>
public sealed class ConversationsHttpTests : IDisposable
{
    private const string ChannelKey = "test-only-channel-service-key-0123456789abcdef";
    private const string DashboardKey = "test-only-dashboard-api-key-0123456789abcdef";
    private const string Instance = "fixture-instance-A";
    private const string SeededJid = "5493815550101@s.whatsapp.net";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"agroflow-conversations-http-{Guid.NewGuid():N}.db");
    private readonly List<IDisposable> _factories = [];

    [Fact]
    public async Task Channel_and_dashboard_routes_require_their_own_key()
    {
        using var client = CreateClient();

        using var anonymousEvent = await client.PostAsync("/api/v1/conversation-events", Json(EventBody("fixture-in-001")));
        using var wrongKeyEvent = await Send(client, HttpMethod.Post, "/api/v1/conversation-events",
            EventBody("fixture-in-001"), ApiKeySchemes.ChannelHeader, "wrong-key-wrong-key-wrong-key-wrong-key");
        using var dashboardKeyEvent = await Send(client, HttpMethod.Post, "/api/v1/conversation-events",
            EventBody("fixture-in-001"), ApiKeySchemes.DashboardHeader, DashboardKey);
        using var anonymousList = await client.GetAsync("/api/v1/conversations");
        using var channelKeyList = await Send(client, HttpMethod.Get, "/api/v1/conversations", null,
            ApiKeySchemes.ChannelHeader, ChannelKey);
        using var anonymousDrivers = await client.GetAsync("/api/v1/drivers");

        Assert.All(
            new[] { anonymousEvent, wrongKeyEvent, dashboardKeyEvent, anonymousList, channelKeyList, anonymousDrivers },
            response =>
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            });
        using var problem = JsonDocument.Parse(await anonymousEvent.Content.ReadAsStringAsync());
        Assert.Equal("UNAUTHENTICATED", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Full_channel_flow_registers_deduplicates_replies_and_is_visible_to_the_dashboard()
    {
        using var client = CreateClient();

        using var created = await Channel(client, HttpMethod.Post, "/api/v1/conversation-events", EventBody("fixture-in-001"));
        using var repeated = await Channel(client, HttpMethod.Post, "/api/v1/conversation-events", EventBody("fixture-in-001"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var root = createdBody.RootElement;
        Assert.Equal("process", root.GetProperty("disposition").GetString());
        Assert.Equal("PROCESSING", root.GetProperty("processing").GetProperty("status").GetString());
        Assert.True(root.GetProperty("sender").GetProperty("recognized").GetBoolean());
        Assert.False(root.GetProperty("sender").TryGetProperty("name", out _));
        var messageId = root.GetProperty("messageId").GetGuid();
        var conversationId = root.GetProperty("conversationId").GetGuid();
        var claimToken = root.GetProperty("processing").GetProperty("claimToken").GetString();

        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        using var repeatedBody = JsonDocument.Parse(await repeated.Content.ReadAsStringAsync());
        Assert.Equal("duplicate_stop", repeatedBody.RootElement.GetProperty("disposition").GetString());
        Assert.Equal(messageId, repeatedBody.RootElement.GetProperty("messageId").GetGuid());
        Assert.False(repeatedBody.RootElement.GetProperty("processing").TryGetProperty("claimToken", out _));

        using var intent = await Channel(client, HttpMethod.Post, "/api/v1/conversation-reply-intents",
            new { inboundMessageId = messageId, claimToken, text = "[SINTÉTICO] respuesta" });
        Assert.Equal(HttpStatusCode.Created, intent.StatusCode);
        using var intentBody = JsonDocument.Parse(await intent.Content.ReadAsStringAsync());
        Assert.Equal("PENDING_SEND", intentBody.RootElement.GetProperty("sendStatus").GetString());
        Assert.Equal(SeededJid, intentBody.RootElement.GetProperty("recipientAddress").GetString());
        Assert.Equal(Instance, intentBody.RootElement.GetProperty("instance").GetString());
        var replyId = intentBody.RootElement.GetProperty("replyId").GetGuid();

        using var accepted = await Channel(client, HttpMethod.Patch,
            $"/api/v1/conversation-reply-intents/{replyId}/send-status",
            new { status = "accepted", providerMessageId = "fixture-out-001" });
        using var invalid = await Channel(client, HttpMethod.Patch,
            $"/api/v1/conversation-reply-intents/{replyId}/send-status", new { status = "failed" });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);
        Assert.Equal("INVALID_SEND_STATUS_TRANSITION", await CodeOf(invalid));

        using var list = await Dashboard(client, HttpMethod.Get, "/api/v1/conversations");
        using var messages = await Dashboard(client, HttpMethod.Get, $"/api/v1/conversations/{conversationId}/messages");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listBody = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var summary = Assert.Single(listBody.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("Transportista Demo Uno", summary.GetProperty("sender").GetProperty("name").GetString());
        Assert.DoesNotContain("5493815550101", summary.GetProperty("remoteAddressMasked").GetString());
        Assert.Equal(JsonValueKind.Null, listBody.RootElement.GetProperty("nextCursor").ValueKind);
        using var messagesBody = JsonDocument.Parse(await messages.Content.ReadAsStringAsync());
        var items = messagesBody.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(new[] { "INBOUND", "OUTBOUND" }, items.Select(m => m.GetProperty("direction").GetString()));
        Assert.Equal("COMPLETED", items[0].GetProperty("processingStatus").GetString());
        Assert.Equal("ACCEPTED", items[1].GetProperty("sendStatus").GetString());
    }

    [Theory]
    [InlineData("ingenioId")]
    [InlineData("tenantId")]
    [InlineData("pushName")]
    public async Task Event_body_rejects_unknown_properties_such_as_a_declared_ingenio(string property)
    {
        using var client = CreateClient();
        var body = JsonSerializer.SerializeToNode(EventBody("fixture-in-001"))!.AsObject();
        body[property] = Guid.NewGuid().ToString();

        using var response = await Channel(client, HttpMethod.Post, "/api/v1/conversation-events", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("VALIDATION_ERROR", problem.RootElement.GetProperty("code").GetString());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(property, out _));
    }

    [Fact]
    public async Task Event_contract_errors_map_to_validation_forbidden_and_conflict()
    {
        using var client = CreateClient();
        await (await Channel(client, HttpMethod.Post, "/api/v1/conversation-events", EventBody("fixture-in-001"))).Content.ReadAsStringAsync();

        using var media = await Send(client, HttpMethod.Post, "/api/v1/conversation-events", null,
            ApiKeySchemes.ChannelHeader, ChannelKey, new StringContent("provider=evolution", Encoding.UTF8, "text/plain"));
        using var audio = await Channel(client, HttpMethod.Post, "/api/v1/conversation-events",
            EventBody("fixture-in-002", contentType: "audio"));
        using var group = await Channel(client, HttpMethod.Post, "/api/v1/conversation-events",
            EventBody("fixture-in-003", remoteAddress: "120363000000000000@g.us"));
        using var unknownInstance = await Channel(client, HttpMethod.Post, "/api/v1/conversation-events",
            EventBody("fixture-in-004", instance: "fixture-unknown"));
        using var conflict = await Channel(client, HttpMethod.Post, "/api/v1/conversation-events",
            EventBody("fixture-in-001", text: "[SINTÉTICO] otro texto"));

        Assert.Equal(HttpStatusCode.BadRequest, media.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, audio.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, group.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unknownInstance.StatusCode);
        Assert.Equal("CHANNEL_INSTANCE_NOT_ALLOWED", await CodeOf(unknownInstance));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("CONVERSATION_EVENT_CONFLICT", await CodeOf(conflict));
        Assert.DoesNotContain("otro texto", await conflict.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Processing_failure_route_accepts_only_the_catalog_and_the_holder_token()
    {
        using var client = CreateClient();
        using var created = await Channel(client, HttpMethod.Post, "/api/v1/conversation-events", EventBody("fixture-in-001"));
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var messageId = body.RootElement.GetProperty("messageId").GetGuid();
        var claimToken = body.RootElement.GetProperty("processing").GetProperty("claimToken").GetString();
        var path = $"/api/v1/conversation-events/{messageId}/processing";

        using var foreignToken = await Channel(client, HttpMethod.Patch, path,
            new { claimToken = "not-the-token", status = "failed", errorCode = "AGENT_ERROR" });
        using var badCode = await Channel(client, HttpMethod.Patch, path,
            new { claimToken, status = "failed", errorCode = "free text" });
        using var completed = await Channel(client, HttpMethod.Patch, path,
            new { claimToken, status = "completed", errorCode = "AGENT_ERROR" });
        using var failed = await Channel(client, HttpMethod.Patch, path,
            new { claimToken, status = "failed", errorCode = "AGENT_ERROR" });

        Assert.Equal(HttpStatusCode.Conflict, foreignToken.StatusCode);
        Assert.Equal("PROCESSING_CLAIM_INVALID", await CodeOf(foreignToken));
        Assert.Equal(HttpStatusCode.BadRequest, badCode.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, completed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
    }

    [Theory]
    [InlineData("?cursor=not-a-cursor")]
    [InlineData("?cursor=MTIzLmFiYw")]
    [InlineData("?limit=0")]
    [InlineData("?limit=51")]
    [InlineData("?page=2")]
    public async Task Malformed_listing_parameters_are_validation_errors(string query)
    {
        using var client = CreateClient();

        using var response = await Dashboard(client, HttpMethod.Get, $"/api/v1/conversations{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await CodeOf(response));
    }

    [Fact]
    public async Task Unknown_conversation_is_not_found()
    {
        using var client = CreateClient();

        using var detail = await Dashboard(client, HttpMethod.Get, $"/api/v1/conversations/{Guid.NewGuid()}");
        using var messages = await Dashboard(client, HttpMethod.Get, $"/api/v1/conversations/{Guid.NewGuid()}/messages");

        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal("CONVERSATION_NOT_FOUND", await CodeOf(detail));
        Assert.Equal(HttpStatusCode.NotFound, messages.StatusCode);
    }

    [Fact]
    public async Task Registered_driver_number_is_recognized_by_the_channel()
    {
        using var client = CreateClient();
        var driver = new { name = "Transportista Alta API", dni = "20555555555", whatsApp = "+5493815550155" };

        using var missingKey = await Dashboard(client, HttpMethod.Post, "/api/v1/drivers", driver);
        using var localFormat = await Dashboard(client, HttpMethod.Post, "/api/v1/drivers",
            new { name = "Sin código", dni = "20666666666", whatsApp = "3815550166" }, idempotencyKey: "fixture-key-0");
        using var created = await Dashboard(client, HttpMethod.Post, "/api/v1/drivers", driver, idempotencyKey: "fixture-key-1");
        using var replay = await Dashboard(client, HttpMethod.Post, "/api/v1/drivers", driver, idempotencyKey: "fixture-key-1");
        using var duplicate = await Dashboard(client, HttpMethod.Post, "/api/v1/drivers",
            new { name = "Otro", dni = "20777777777", whatsApp = "+5493815550155" }, idempotencyKey: "fixture-key-2");
        using var message = await Channel(client, HttpMethod.Post, "/api/v1/conversation-events",
            EventBody("fixture-in-001", remoteAddress: "5493815550155@s.whatsapp.net"));

        Assert.Equal(HttpStatusCode.BadRequest, missingKey.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, localFormat.StatusCode);
        using var localFormatProblem = JsonDocument.Parse(await localFormat.Content.ReadAsStringAsync());
        Assert.True(localFormatProblem.RootElement.GetProperty("errors").TryGetProperty("whatsApp", out _));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var receipt = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var driverId = receipt.RootElement.GetProperty("resourceId").GetGuid();
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        using var replayReceipt = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        Assert.Equal(driverId, replayReceipt.RootElement.GetProperty("resourceId").GetGuid());
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("DUPLICATE_WHATSAPP", await CodeOf(duplicate));
        using var messageBody = JsonDocument.Parse(await message.Content.ReadAsStringAsync());
        Assert.True(messageBody.RootElement.GetProperty("sender").GetProperty("recognized").GetBoolean());
        Assert.Equal(driverId, messageBody.RootElement.GetProperty("sender").GetProperty("transportistaId").GetGuid());

        using var list = await Dashboard(client, HttpMethod.Get, "/api/v1/drivers?active=true");
        using var listBody = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Contains(listBody.RootElement.EnumerateArray(), d => d.GetProperty("id").GetGuid() == driverId
            && d.GetProperty("whatsApp").GetString() == "+5493815550155");
    }

    [Fact]
    public async Task Missing_keys_keep_the_demo_running_with_closed_routes()
    {
        using var client = CreateClient(channelKey: null, dashboardKey: null);

        using var health = await client.GetAsync("/health");
        using var appointments = await client.GetAsync("/api/v1/appointments");
        using var events = await Send(client, HttpMethod.Post, "/api/v1/conversation-events",
            EventBody("fixture-in-001"), ApiKeySchemes.ChannelHeader, ChannelKey);
        using var conversations = await Send(client, HttpMethod.Get, "/api/v1/conversations", null,
            ApiKeySchemes.DashboardHeader, DashboardKey);

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, appointments.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, events.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, conversations.StatusCode);
    }

    [Theory]
    [InlineData("short-key", DashboardKey)]
    [InlineData(DashboardKey, DashboardKey)]
    public void Weak_or_shared_keys_abort_startup(string channelKey, string dashboardKey)
    {
        var exception = Assert.ThrowsAny<Exception>(() => CreateClient(channelKey, dashboardKey));

        Assert.IsType<InvalidOperationException>(exception.GetBaseException());
        Assert.DoesNotContain(channelKey, exception.GetBaseException().Message);
    }

    [Fact]
    public async Task OpenAPI_document_lists_channel_conversation_and_driver_routes()
    {
        using var client = CreateClient();

        using var swagger = await client.GetAsync("/swagger/v1/swagger.json");

        using var document = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Contains("/api/v1/conversation-events", paths);
        Assert.Contains("/api/v1/conversation-reply-intents", paths);
        Assert.Contains("/api/v1/conversations", paths);
        Assert.Contains("/api/v1/drivers", paths);
        Assert.DoesNotContain(paths, path => path.Contains("/trucks", StringComparison.Ordinal));
        Assert.DoesNotContain(paths, path => path.Contains("/farms", StringComparison.Ordinal));
    }

    private static object EventBody(
        string providerMessageId,
        string instance = Instance,
        string remoteAddress = SeededJid,
        string contentType = "text",
        string text = "[SINTÉTICO] mensaje de prueba") => new
        {
            provider = "evolution",
            instance,
            providerMessageId,
            remoteAddress,
            providerTimestamp = "2026-01-01T12:00:00Z",
            contentType,
            text
        };

    private HttpClient CreateClient(string? channelKey = ChannelKey, string? dashboardKey = DashboardKey)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("LocalDemo:Enabled", "true");
            builder.UseSetting("LocalDemo:WhatsAppInstance", Instance);
            builder.UseSetting("ConnectionStrings:AgroFlowDb", $"Data Source={_dbPath};Default Timeout=5");
            builder.UseSetting(ApiKeySchemes.ChannelKeySetting, channelKey ?? string.Empty);
            builder.UseSetting(ApiKeySchemes.DashboardKeySetting, dashboardKey ?? string.Empty);
        });
        _factories.Add(factory);
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static Task<HttpResponseMessage> Channel(HttpClient client, HttpMethod method, string path, object? body) =>
        Send(client, method, path, body, ApiKeySchemes.ChannelHeader, ChannelKey);

    private static Task<HttpResponseMessage> Dashboard(
        HttpClient client, HttpMethod method, string path, object? body = null, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = body is null ? null : Json(body) };
        request.Headers.Add(ApiKeySchemes.DashboardHeader, DashboardKey);
        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> Send(
        HttpClient client, HttpMethod method, string path, object? body, string header, string key,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content ?? (body is null ? null : Json(body)) };
        request.Headers.Add(header, key);
        return client.SendAsync(request);
    }

    private static JsonContent Json(object body) => JsonContent.Create(body);

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("code").GetString();
    }

    public void Dispose()
    {
        foreach (var factory in _factories)
            factory.Dispose();
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
        {
            var path = _dbPath + suffix;
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
