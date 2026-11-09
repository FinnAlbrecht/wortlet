using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wortlet.Application.Recognition;
using Wortlet.Infrastructure.OpenRouter;

namespace Wortlet.UnitTests.Infrastructure;

public class OpenRouterVisionClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, string, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Calls.Add((request, body));
            return respond(request, body);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static OpenRouterVisionClient Client(StubHandler handler, string? key = "sk-test") =>
        new(new HttpClient(handler), Options.Create(new OpenRouterOptions
        {
            ApiKey = key,
            Model = "model-a",
            FallbackModels = "model-b, model-c",
            Referer = "https://example.test",
        }), NullLogger<OpenRouterVisionClient>.Instance);

    private static readonly VisionRequest Request = new("Prompt", [1, 2, 3], "image/jpeg");

    [Fact]
    public async Task Sends_headers_and_image_as_data_url()
    {
        var handler = new StubHandler((_, _) => Json(HttpStatusCode.OK, """{"choices":[{"message":{"content":"{\"chapters\":[]}"}}]}"""));
        var result = await Client(handler).CompleteAsync(Request);

        Assert.Equal("model-a", result.Model);
        var (req, body) = Assert.Single(handler.Calls);
        Assert.Equal("Bearer sk-test", req.Headers.Authorization!.ToString());
        Assert.Equal("Vokabeltrainer", req.Headers.GetValues("X-Title").Single());
        Assert.Equal("https://example.test", req.Headers.GetValues("HTTP-Referer").Single());
        Assert.EndsWith("/chat/completions", req.RequestUri!.AbsoluteUri);
        Assert.Contains("data:image/jpeg;base64,AQID", body);
        Assert.Contains("\"image_url\"", body);
        Assert.Contains("\"model\":\"model-a\"", body);
    }

    [Fact]
    public async Task Falls_back_to_next_model_when_unavailable()
    {
        var handler = new StubHandler((_, body) =>
            body.Contains("model-a") ? Json(HttpStatusCode.NotFound, """{"error":{"code":404,"message":"No endpoints found"}}""")
            : body.Contains("model-b") ? Json(HttpStatusCode.TooManyRequests, """{"error":{"code":429,"message":"model-b is temporarily rate-limited upstream"}}""")
            : Json(HttpStatusCode.OK, """{"choices":[{"message":{"content":"ok"}}]}"""));

        var result = await Client(handler).CompleteAsync(Request);
        Assert.Equal("model-c", result.Model);
        Assert.Equal(3, handler.Calls.Count);
    }

    [Fact]
    public async Task Daily_free_quota_stops_immediately()
    {
        var handler = new StubHandler((_, _) => Json(HttpStatusCode.TooManyRequests,
            """{"error":{"code":429,"message":"Rate limit exceeded: free-models-per-day. Add 10 credits to unlock 1000 free model requests per day"}}"""));

        var ex = await Assert.ThrowsAsync<RecognitionException>(() => Client(handler).CompleteAsync(Request));
        Assert.Equal(RecognitionFailure.ProviderQuotaExhausted, ex.Failure);
        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task All_models_unavailable_gives_german_message()
    {
        var handler = new StubHandler((_, _) => Json(HttpStatusCode.ServiceUnavailable, """{"error":{"code":503,"message":"down"}}"""));
        var ex = await Assert.ThrowsAsync<RecognitionException>(() => Client(handler).CompleteAsync(Request));
        Assert.Equal(RecognitionFailure.ModelsUnavailable, ex.Failure);
        Assert.Contains("kein Bilderkennungs-Modell", ex.Message);
        Assert.Equal(3, handler.Calls.Count);
    }

    [Fact]
    public async Task Error_inside_200_response_is_treated_as_failure()
    {
        var handler = new StubHandler((_, body) => body.Contains("model-a")
            ? Json(HttpStatusCode.OK, """{"error":{"code":502,"message":"Provider returned error"}}""")
            : Json(HttpStatusCode.OK, """{"choices":[{"message":{"content":[{"type":"text","text":"hallo"}]}}]}"""));
        var result = await Client(handler).CompleteAsync(Request);
        Assert.Equal("model-b", result.Model);
        Assert.Equal("hallo", result.Content);
    }

    [Fact]
    public void Not_configured_without_key()
    {
        Assert.False(Client(new StubHandler((_, _) => Json(HttpStatusCode.OK, "{}")), key: null).IsConfigured);
    }
}
