namespace Wortlet.Infrastructure.OpenRouter;

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    /// <summary>NUR per User Secrets (Entwicklung) oder Umgebungsvariable OpenRouter__ApiKey (Produktion).</summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";

    public string Model { get; set; } = "google/gemma-4-31b-it:free";

    /// <summary>Kommagetrennt. Wird der Reihe nach versucht, wenn ein Modell nicht verfügbar ist.</summary>
    public string FallbackModels { get; set; } = "google/gemma-4-26b-a4b-it:free,nvidia/nemotron-3.5-super-vl:free,openrouter/free";

    /// <summary>Wird als Header „HTTP-Referer“ mitgeschickt (Adresse der App).</summary>
    public string Referer { get; set; } = "http://localhost:8080";

    public string Title { get; set; } = "Vokabeltrainer";

    public int TimeoutSeconds { get; set; } = 120;

    public IReadOnlyList<string> AllModels() =>
        new[] { Model }
            .Concat((FallbackModels ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
}
