using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Application.Auth;
using Wortlet.Application.Common;
using Wortlet.Application.Recognition;
using Wortlet.Domain.Common;

namespace Wortlet.Api.Common;

/// <summary>Wandelt Ausnahmen in ProblemDetails mit verständlicher deutscher Meldung um.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, message, code) = exception switch
        {
            ValidationException e => (StatusCodes.Status400BadRequest, e.Message, "validation"),
            NotFoundException e => (StatusCodes.Status404NotFound, e.Message, "not_found"),
            ConflictException e => (StatusCodes.Status409Conflict, e.Message, "conflict"),
            AuthenticationFailedException e => (StatusCodes.Status401Unauthorized, e.Message, "unauthorized"),
            DomainException e => (StatusCodes.Status400BadRequest, e.Message, "domain"),
            RecognitionException e => (RecognitionStatus(e.Failure), e.Message, "recognition_" + ToSnake(e.Failure.ToString())),
            BadHttpRequestException e when e.StatusCode == StatusCodes.Status413PayloadTooLarge =>
                (StatusCodes.Status413PayloadTooLarge, "Die Datei ist zu gross.", "too_large"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Die Anfrage war ungültig.", "bad_request"),
            OperationCanceledException when context.RequestAborted.IsCancellationRequested => (499, "Abgebrochen.", "cancelled"),
            _ => (StatusCodes.Status500InternalServerError, "Ein unerwarteter Fehler ist aufgetreten. Bitte versuche es nochmals.", "server_error"),
        };

        if (status >= 500)
            logger.LogError(exception, "Unbehandelter Fehler bei {Path}", context.Request.Path);
        else
            logger.LogInformation("Anfrage {Path} abgelehnt ({Status}): {Message}", context.Request.Path, status, message);

        context.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Title = message, Detail = message };
        details.Extensions["code"] = code;
        if (exception is ValidationException { Errors.Count: > 0 } ve)
            details.Extensions["errors"] = ve.Errors;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = details,
            Exception = exception,
        });
    }

    private static int RecognitionStatus(RecognitionFailure failure) => failure switch
    {
        RecognitionFailure.NotConfigured => StatusCodes.Status503ServiceUnavailable,
        RecognitionFailure.InvalidImage => StatusCodes.Status400BadRequest,
        RecognitionFailure.UserLimitReached => StatusCodes.Status429TooManyRequests,
        RecognitionFailure.ProviderQuotaExhausted => StatusCodes.Status429TooManyRequests,
        RecognitionFailure.ModelsUnavailable => StatusCodes.Status503ServiceUnavailable,
        RecognitionFailure.Unreadable => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status502BadGateway,
    };

    private static string ToSnake(string s) =>
        string.Concat(s.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
