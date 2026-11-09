using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Wortlet.IntegrationTests;

public class RecognitionFailureTests : IClassFixture<WortletFactory>
{
    private readonly WortletFactory _factory;

    public RecognitionFailureTests(WortletFactory factory) => _factory = factory;

    private static MultipartFormDataContent Photo()
    {
        var image = new ByteArrayContent([0xFF, 0xD8, 0xFF]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return new MultipartFormDataContent { { image, "photo", "x.jpg" } };
    }

    [Fact]
    public async Task Unavailable_models_and_unreadable_photos_give_german_errors()
    {
        var client = await _factory.CreateUserClientAsync();

        _factory.OpenRouter.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\":{\"code\":404,\"message\":\"No endpoints found\"}}", Encoding.UTF8, "application/json"),
        };
        var unavailable = await client.PostAsync("/api/recognize", Photo());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.Contains("kein Bilderkennungs-Modell", (await unavailable.Content.ReadFromJsonAsync<ProblemJson>())!.Title);

        _factory.OpenRouter.Respond = _ => FakeOpenRouter.Ok("Ich kann auf dem Bild leider nichts erkennen.");
        var unreadable = await client.PostAsync("/api/recognize", Photo());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unreadable.StatusCode);
        Assert.Contains("keine Wörter", (await unreadable.Content.ReadFromJsonAsync<ProblemJson>())!.Title);

        var notImage = new MultipartFormDataContent { { new StringContent("hallo"), "photo", "x.txt" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/recognize", notImage)).StatusCode);
    }
}
