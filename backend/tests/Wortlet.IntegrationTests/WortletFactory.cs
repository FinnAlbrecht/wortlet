using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wortlet.Application.Auth;
using Wortlet.Application.Recognition;
using Wortlet.Infrastructure.OpenRouter;

namespace Wortlet.IntegrationTests;

/// <summary>Startet die echte API mit eigener SQLite-Datei und einem simulierten OpenRouter.</summary>
public sealed class WortletFactory : WebApplicationFactory<Program>
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "wortlet-tests-" + Guid.NewGuid().ToString("N"));

    public FakeOpenRouter OpenRouter { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Storage:DataDirectory", _dataDir);
        builder.UseSetting("Jwt:Key", "integration-test-signing-key-0123456789-abcdefghij");
        builder.UseSetting("OpenRouter:ApiKey", "sk-or-test");
        builder.UseSetting("OpenRouter:Model", "test/model-a");
        builder.UseSetting("OpenRouter:FallbackModels", "test/model-b");
        builder.UseSetting("Recognition:DailyLimitPerUser", "2");
        builder.UseSetting("RateLimiting:AuthPerMinute", "1000");
        builder.UseSetting("Backup:Enabled", "false"); // Sicherungen werden gezielt in BackupTests geprüft
        builder.ConfigureServices(services =>
            services.AddHttpClient<IVisionModelClient, OpenRouterVisionClient>()
                .ConfigurePrimaryHttpMessageHandler(() => OpenRouter));
    }

    public async Task<HttpClient> CreateUserClientAsync(string? email = null)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email ?? $"user-{Guid.NewGuid():N}@example.test", "geheim1234"));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { Directory.Delete(_dataDir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

/// <summary>Simuliert https://openrouter.ai/api/v1/chat/completions.</summary>
public sealed class FakeOpenRouter : HttpMessageHandler
{
    public List<string> RequestBodies { get; } = [];
    public List<HttpRequestMessage> Requests { get; } = [];

    public Func<string, HttpResponseMessage> Respond { get; set; } = _ => Ok(
        "Hier ist das Ergebnis:\n```json\n{\"chapters\":[{\"name\":\"Obst\",\"words\":[{\"de\":\"die Birne\",\"en\":[\"pear\"]},{\"de\":\"die Kasse\",\"en\":[\"checkout\",\"till\"]}]}]}\n```");

    public static HttpResponseMessage Ok(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { choices = new[] { new { message = new { role = "assistant", content } } } }),
            Encoding.UTF8, "application/json"),
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
        lock (RequestBodies)
        {
            RequestBodies.Add(body);
            Requests.Add(request);
        }
        return Respond(body);
    }
}
