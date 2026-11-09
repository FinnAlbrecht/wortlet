using Microsoft.AspNetCore.Mvc;

namespace Wortlet.Api.Common;

public static class ValidationResponses
{
    /// <summary>Ungültige Eingaben (Model-Validierung) → 400 mit der ersten Meldung als Titel.</summary>
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.Select(x => Friendly(x.ErrorMessage)).ToArray());
        var first = errors.Values.SelectMany(v => v).FirstOrDefault() ?? "Bitte prüfe deine Eingaben.";
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = first,
            Detail = first,
        };
        problem.Extensions["code"] = "validation";
        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    // Technische Meldungen (z. B. kaputtes JSON) nicht anzeigen.
    private static string Friendly(string message) =>
        string.IsNullOrWhiteSpace(message) || message.Contains("JSON", StringComparison.Ordinal) || message.StartsWith("The ", StringComparison.Ordinal)
            ? "Die Angaben sind ungültig."
            : message;
}
