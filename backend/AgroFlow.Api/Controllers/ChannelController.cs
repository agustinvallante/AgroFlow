using System.Globalization;
using System.Text.RegularExpressions;
using AgroFlow.Api.Contracts.Conversations;
using AgroFlow.Api.Security;
using AgroFlow.Application.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Operaciones del canal de WhatsApp para n8n, autenticado como servicio. El
/// ingenio sale siempre de la instancia registrada, nunca del cuerpo. Los logs
/// sólo llevan códigos: ni texto, ni direcciones, ni teléfonos.
/// </summary>
[Route("api/v1")]
[Authorize(Policy = ApiKeyPolicies.ChannelIngest)]
public sealed class ChannelController : StrictJsonControllerBase
{
    private static readonly Regex UtcDateTimePattern = new(
        @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:[Zz]|[+-]\d{2}:\d{2})\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly HashSet<string> EventBodyKeys = new(
        ["provider", "instance", "providerMessageId", "remoteAddress", "providerTimestamp", "contentType", "text"],
        StringComparer.Ordinal);

    private static readonly HashSet<string> ReplyIntentBodyKeys = new(
        ["inboundMessageId", "claimToken", "text"], StringComparer.Ordinal);

    private static readonly HashSet<string> SendStatusBodyKeys = new(
        ["status", "providerMessageId"], StringComparer.Ordinal);

    private static readonly HashSet<string> ProcessingBodyKeys = new(
        ["claimToken", "status", "errorCode"], StringComparer.Ordinal);

    private readonly ConversationService _service;
    private readonly ILogger<ChannelController>? _logger;

    public ChannelController(ConversationService service, ILogger<ChannelController>? logger = null)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("conversation-events")]
    public async Task<IActionResult> RegisterEvent(CancellationToken cancellationToken)
    {
        var (request, failure) = await ReadRequestBodyAsync<ConversationEventRequest>(EventBodyKeys, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!string.Equals(request!.ContentType, "text", StringComparison.Ordinal))
        {
            errors["contentType"] = ["Solo se admite text."];
        }

        DateTime? providerTimestamp = null;
        if (request.ProviderTimestamp is not null)
        {
            if (UtcDateTimePattern.IsMatch(request.ProviderTimestamp) &&
                DateTimeOffset.TryParse(request.ProviderTimestamp, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed))
            {
                providerTimestamp = parsed.UtcDateTime;
            }
            else
            {
                errors["providerTimestamp"] = ["Debe ser una fecha y hora ISO 8601 con desplazamiento horario explícito."];
            }
        }

        if (errors.Count > 0)
        {
            return ProblemResult(400, ValidationErrorCode, errors);
        }

        return await ExecuteAsync(async () =>
        {
            var registration = await _service.IngestAsync(
                new RegisterInboundCommand(
                    request.Provider!,
                    request.Instance!,
                    request.ProviderMessageId!,
                    request.RemoteAddress!,
                    providerTimestamp,
                    request.Text!),
                cancellationToken);
            var body = ConversationHttpMapper.ToResponse(registration);
            return registration.Created
                ? Created($"/api/v1/conversations/{registration.ConversationId:D}", body)
                : Ok(body);
        });
    }

    [HttpPatch("conversation-events/{messageId}/processing")]
    public async Task<IActionResult> ReportProcessing(string messageId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(messageId, out var inboundMessageId))
        {
            return ValidationProblemFor("messageId", "Debe ser un UUID válido.");
        }

        var (request, failure) = await ReadRequestBodyAsync<ProcessingResultRequest>(ProcessingBodyKeys, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        // Los cierres exitosos llegan por el estado de envío de la respuesta.
        if (!string.Equals(request!.Status, "failed", StringComparison.Ordinal))
        {
            return ValidationProblemFor("status", "Solo se admite failed.");
        }

        return await ExecuteAsync(async () =>
        {
            var result = await _service.ReportProcessingFailureAsync(
                new ReportProcessingFailureCommand(inboundMessageId, request.ClaimToken!, request.ErrorCode!),
                cancellationToken);
            return Ok(ConversationHttpMapper.ToResponse(result));
        });
    }

    [HttpPost("conversation-reply-intents")]
    public async Task<IActionResult> CreateReplyIntent(CancellationToken cancellationToken)
    {
        var (request, failure) = await ReadRequestBodyAsync<ReplyIntentRequest>(ReplyIntentBodyKeys, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (!Guid.TryParse(request!.InboundMessageId, out var inboundMessageId))
        {
            return ValidationProblemFor("inboundMessageId", "Debe ser un UUID válido.");
        }

        return await ExecuteAsync(async () =>
        {
            var intent = await _service.CreateReplyIntentAsync(
                new CreateReplyIntentCommand(inboundMessageId, request.ClaimToken!, request.Text!),
                cancellationToken);
            var body = ConversationHttpMapper.ToResponse(intent);
            return intent.Created
                ? Created($"/api/v1/conversation-reply-intents/{intent.ReplyId:D}", body)
                : Ok(body);
        });
    }

    [HttpPatch("conversation-reply-intents/{replyId}/send-status")]
    public async Task<IActionResult> UpdateSendStatus(string replyId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(replyId, out var parsedReplyId))
        {
            return ValidationProblemFor("replyId", "Debe ser un UUID válido.");
        }

        var (request, failure) = await ReadRequestBodyAsync<SendStatusRequest>(SendStatusBodyKeys, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (!ConversationHttpMapper.TryParseReportedSendStatus(request!.Status, out var status))
        {
            return ValidationProblemFor("status", "Debe ser accepted, failed o unknown.");
        }

        return await ExecuteAsync(async () =>
        {
            var intent = await _service.UpdateSendStatusAsync(
                new UpdateSendStatusCommand(parsedReplyId, status, request.ProviderMessageId),
                cancellationToken);
            return Ok(ConversationHttpMapper.ToResponse(intent));
        });
    }

    protected override string TitleFor(string code) => code switch
    {
        ConversationErrorCodes.ChannelInstanceNotAllowed => "Instancia del canal no habilitada",
        ConversationErrorCodes.ConversationEventConflict => "El mensaje ya fue registrado con otro contenido",
        ConversationErrorCodes.MessageNotFound => "Mensaje entrante inexistente",
        ConversationErrorCodes.ProcessingClaimInvalid => "Autorización de procesamiento inválida o vencida",
        ConversationErrorCodes.ReplyIntentConflict => "El mensaje ya tiene otra respuesta",
        ConversationErrorCodes.ReplyNotFound => "Respuesta inexistente",
        ConversationErrorCodes.InvalidSendStatusTransition => "Transición de estado de envío inválida",
        _ => "Error interno"
    };

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
                ConversationErrorCodes.ChannelInstanceNotAllowed => 403,
                ConversationErrorCodes.MessageNotFound or ConversationErrorCodes.ReplyNotFound => 404,
                ConversationErrorCodes.ConversationEventConflict or ConversationErrorCodes.ProcessingClaimInvalid or
                    ConversationErrorCodes.ReplyIntentConflict or ConversationErrorCodes.InvalidSendStatusTransition => 409,
                _ => 500
            };

            if (status == 500)
            {
                _logger?.LogError("Código de error no reconocido del canal: {Code}", exception.Code);
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
            // Sólo el tipo: la excepción de persistencia podría citar valores.
            _logger?.LogError("Error inesperado en el canal de WhatsApp: {ExceptionType}", exception.GetType().Name);
            return ProblemResult(500, InternalErrorCode);
        }
    }
}
