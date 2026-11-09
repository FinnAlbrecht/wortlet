using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wortlet.Application.Recognition;

namespace Wortlet.Infrastructure.OpenRouter;

/// <summary>
/// Ruft https://openrouter.ai/api/v1/chat/completions mit dem Foto als Data-URL auf.
/// Ist ein Modell nicht verfügbar, wird automatisch das nächste aus der Konfiguration versucht.
/// </summary>
public sealed class OpenRouterVisionClient(
    HttpClient http,
    IOptions<OpenRouterOptions> options,
    ILogger<OpenRouterVisionClient> logger) : IVisionModelClient
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<VisionCompletion> CompleteAsync(VisionRequest request, CancellationToken ct = default)
    {
        var o = options.Value;
        if (!IsConfigured)
            throw new RecognitionException(RecognitionFailure.NotConfigured, "Die Bilderkennung ist auf dem Server nicht eingerichtet.");

        var dataUrl = $"data:{request.MediaType};base64,{Convert.ToBase64String(request.Image)}";
        var endpoint = o.BaseUrl.TrimEnd('/') + "/chat/completions";

        foreach (var model in o.AllModels())
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey);
            message.Headers.TryAddWithoutValidation("HTTP-Referer", o.Referer);
            message.Headers.TryAddWithoutValidation("X-Title", o.Title);
            message.Content = JsonContent.Create(new
            {
                model,
                temperature = 0,
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = request.Prompt },
                            new { type = "image_url", image_url = new { url = dataUrl } },
                        },
                    },
                },
            });

            HttpResponseMessage response;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, o.TimeoutSeconds)));
            try
            {
                response = await http.SendAsync(message, timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("OpenRouter: Zeitüberschreitung bei {Model}, versuche nächstes Modell.", model);
                continue;
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "OpenRouter nicht erreichbar ({Model}).", model);
                continue;
            }

            using (response)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var (errorCode, errorMessage) = ReadError(body);
                var status = errorCode ?? (int)response.StatusCode;

                if (response.IsSuccessStatusCode && errorMessage is null)
                {
                    var content = ReadContent(body);
                    if (!string.IsNullOrWhiteSpace(content))
                        return new VisionCompletion(content, model);
                    logger.LogWarning("OpenRouter: leere Antwort von {Model}.", model);
                    continue;
                }

                logger.LogWarning("OpenRouter-Fehler {Status} bei {Model}: {Message}", status, model, errorMessage);
                switch (status)
                {
                    case 401:
                        throw new RecognitionException(RecognitionFailure.ProviderError,
                            "Der OpenRouter-Schlüssel auf dem Server ist ungültig. Bitte prüfe die Einstellung OpenRouter:ApiKey.");
                    case 402:
                        throw new RecognitionException(RecognitionFailure.ProviderQuotaExhausted,
                            "Das Guthaben bzw. Ausgabelimit bei OpenRouter ist aufgebraucht. Nutze bis dahin „Prompt kopieren“.");
                    case 429 when IsDailyQuota(errorMessage):
                        throw new RecognitionException(RecognitionFailure.ProviderQuotaExhausted,
                            "Das tägliche Gratis-Kontingent von OpenRouter ist aufgebraucht (es gilt für alle Benutzer zusammen). Morgen geht es wieder – bis dahin hilft „Prompt kopieren“.");
                    default:
                        // 400 (Modell unbekannt), 404 (keine Endpunkte), 408, 429 (Modell überlastet), 5xx → nächstes Modell
                        continue;
                }
            }
        }

        throw new RecognitionException(RecognitionFailure.ModelsUnavailable,
            "Gerade ist kein Bilderkennungs-Modell verfügbar. Versuche es in ein paar Minuten nochmals oder nutze „Prompt kopieren“.");
    }

    internal static bool IsDailyQuota(string? message) =>
        message is not null &&
        (message.Contains("per-day", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("per day", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("daily", StringComparison.OrdinalIgnoreCase));

    internal static (int? Code, string? Message) ReadError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.Object)
            {
                int? code = error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number && c.TryGetInt32(out var n) ? n : null;
                var msg = error.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : "Unbekannter Fehler";
                return (code, msg ?? "Unbekannter Fehler");
            }
        }
        catch (JsonException)
        {
            return (null, string.IsNullOrWhiteSpace(body) ? null : "Ungültige Antwort");
        }
        return (null, null);
    }

    internal static string? ReadContent(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
                return null;
            var first = choices[0];
            if (!first.TryGetProperty("message", out var message) || !message.TryGetProperty("content", out var content))
                return null;
            return content.ValueKind switch
            {
                JsonValueKind.String => content.GetString(),
                JsonValueKind.Array => string.Concat(content.EnumerateArray()
                    .Where(p => p.ValueKind == JsonValueKind.Object && p.TryGetProperty("text", out _))
                    .Select(p => p.GetProperty("text").GetString())),
                _ => null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
