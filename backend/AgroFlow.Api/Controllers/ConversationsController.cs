using System.Globalization;
using AgroFlow.Api.Contracts.Conversations;
using AgroFlow.Api.Security;
using AgroFlow.Application.Conversations;
using AgroFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Consultas de conversaciones del ingenio para el dashboard. Sólo leen: nunca
/// modifican mensajes, autorizaciones ni estados. Un recurso de otro ingenio
/// responde 404 para no revelar su existencia.
/// </summary>
[Route("api/v1/conversations")]
[Authorize(Policy = ApiKeyPolicies.DashboardAccess)]
public sealed class ConversationsController : StrictJsonControllerBase
{
    public const int DefaultPageSize = 20;

    private static readonly HashSet<string> PageQueryKeys = new(["cursor", "limit"], StringComparer.Ordinal);

    private readonly ConversationService _service;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ConversationsController>? _logger;

    public ConversationsController(
        ConversationService service,
        ITenantContext tenant,
        ILogger<ConversationsController>? logger = null)
    {
        _service = service;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var (page, failure) = ReadPage();
        if (failure is not null)
        {
            return failure;
        }

        return await ExecuteAsync(async () =>
        {
            var result = await _service.ListConversationsAsync(_tenant.IngenioId, page!, cancellationToken);
            return Ok(new PageResponse<ConversationSummaryResponse>(
                result.Items.Select(ConversationHttpMapper.ToResponse).ToArray(),
                result.Next is null ? null : PageCursorCodec.Encode(result.Next)));
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var conversationId))
        {
            return ValidationProblemFor("id", "Debe ser un UUID válido.");
        }

        if (RejectUnknownQueryKeys(new HashSet<string>()) is { } failure)
        {
            return failure;
        }

        return await ExecuteAsync(async () =>
            Ok(ConversationHttpMapper.ToResponse(
                await _service.GetConversationAsync(_tenant.IngenioId, conversationId, cancellationToken))));
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> Messages(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var conversationId))
        {
            return ValidationProblemFor("id", "Debe ser un UUID válido.");
        }

        var (page, failure) = ReadPage();
        if (failure is not null)
        {
            return failure;
        }

        return await ExecuteAsync(async () =>
        {
            var result = await _service.ListMessagesAsync(_tenant.IngenioId, conversationId, page!, cancellationToken);
            return Ok(new PageResponse<ConversationMessageResponse>(
                result.Items.Select(ConversationHttpMapper.ToResponse).ToArray(),
                result.Next is null ? null : PageCursorCodec.Encode(result.Next)));
        });
    }

    protected override string TitleFor(string code) => code switch
    {
        ConversationErrorCodes.ConversationNotFound => "Conversación inexistente",
        _ => "Error interno"
    };

    private (PageRequest? Page, IActionResult? Failure) ReadPage()
    {
        if (RejectUnknownQueryKeys(PageQueryKeys) is { } failure)
        {
            return (null, failure);
        }

        var limit = DefaultPageSize;
        if (Request.Query.TryGetValue("limit", out var limitValues) &&
            (!int.TryParse(limitValues[0], NumberStyles.None, CultureInfo.InvariantCulture, out limit) ||
             limit is < 1 or > ConversationService.MaxPageSize))
        {
            return (null, ValidationProblemFor("limit", $"Debe ser un entero entre 1 y {ConversationService.MaxPageSize}."));
        }

        PageCursor? after = null;
        if (Request.Query.TryGetValue("cursor", out var cursorValues) &&
            !PageCursorCodec.TryDecode(cursorValues[0], out after))
        {
            return (null, ValidationProblemFor("cursor", "Cursor inválido."));
        }

        return (new PageRequest(after, limit), null);
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ConversationServiceException exception)
        {
            var status = exception.Code switch
            {
                ConversationErrorCodes.ValidationError => 400,
                ConversationErrorCodes.ConversationNotFound => 404,
                _ => 500
            };

            if (status == 500)
            {
                _logger?.LogError("Código de error no reconocido de conversaciones: {Code}", exception.Code);
                return ProblemResult(500, InternalErrorCode);
            }

            return ProblemResult(status, exception.Code, exception.Errors);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger?.LogError("Error inesperado en la consulta de conversaciones: {ExceptionType}", exception.GetType().Name);
            return ProblemResult(500, InternalErrorCode);
        }
    }
}
