using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Wortlet.IntegrationTests;

public class ApiTests(WortletFactory factory) : IClassFixture<WortletFactory>
{
    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {text}");
        return JsonSerializer.Deserialize<T>(text, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private static async Task<(LanguageJson Language, FolderSummaryJson Folder)> SeedFolder(HttpClient client)
    {
        var languages = await Read<List<LanguageJson>>(await client.GetAsync("/api/languages"));
        var english = Assert.Single(languages);
        var folders = await Read<List<FolderSummaryJson>>(await client.GetAsync($"/api/languages/{english.Id}/folders"));
        return (english, Assert.Single(folders));
    }

    [Fact]
    public async Task New_user_gets_the_seed_folder()
    {
        var client = await factory.CreateUserClientAsync();
        var (english, summary) = await SeedFolder(client);

        Assert.Equal("Englisch", english.Name);
        Assert.Equal(319, english.WordCount);
        Assert.Equal("Einkaufen & Ernährung", summary.Name);
        Assert.Equal(9, summary.ChapterCount);
        Assert.True(summary.IsSeed);

        var folder = await Read<FolderJson>(await client.GetAsync($"/api/folders/{summary.Id}"));
        Assert.Equal("Alte Wörter (S. 201–215)", folder.OldLabel);
        Assert.Equal(7, folder.Chapters.Count(c => c.IsNew));
        Assert.Contains("# Obst und Gemüse | S. 216–217 | neu", folder.Text);
    }

    [Fact]
    public async Task Requests_without_login_are_rejected()
    {
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/languages")).StatusCode);
        using var form = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3]), "photo", "a.jpg" } };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/recognize", form)).StatusCode);
    }

    [Fact]
    public async Task Login_works_and_wrong_password_gives_german_error()
    {
        var email = $"login-{Guid.NewGuid():N}@example.test";
        await factory.CreateUserClientAsync(email);
        var client = factory.CreateClient();

        var ok = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "geheim1234" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var bad = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "falsch" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        var problem = await bad.Content.ReadFromJsonAsync<ProblemJson>();
        Assert.Equal("E-Mail-Adresse oder Passwort ist falsch.", problem!.Title);

        var dup = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "geheim1234" });
        Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);
        Assert.Contains("schon ein Konto", (await dup.Content.ReadFromJsonAsync<ProblemJson>())!.Title);
    }

    [Fact]
    public async Task Users_only_see_their_own_data()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();
        var (_, aliceFolder) = await SeedFolder(alice);

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/folders/{aliceFolder.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/folders/{aliceFolder.Id}")).StatusCode);
    }

    [Fact]
    public async Task Wrong_answer_goes_to_weak_list_and_override_takes_it_out()
    {
        var client = await factory.CreateUserClientAsync();
        var (_, summary) = await SeedFolder(client);
        var url = $"/api/folders/{summary.Id}/quiz";

        var wrong = await Read<AnswerJson>(await client.PostAsJsonAsync($"{url}/answers",
            new { wordKey = "die Sahne", direction = "toForeign", input = "creme", skipped = false, round = 1, practiceMode = false }));
        Assert.False(wrong.Correct);
        Assert.Equal(1, wrong.WeakCount);

        var folder = await Read<FolderJson>(await client.GetAsync($"/api/folders/{summary.Id}"));
        Assert.Equal(new[] { "die Sahne" }, folder.WeakWords);

        var undone = await Read<WeakCountJson>(await client.PostAsJsonAsync($"{url}/override",
            new { wordKey = "die Sahne", round = 1, practiceMode = false, wasWeakBefore = wrong.WasWeakBefore }));
        Assert.Equal(0, undone.WeakCount);

        var reverse = await Read<AnswerJson>(await client.PostAsJsonAsync($"{url}/answers",
            new { wordKey = "die Sahne", direction = "toGerman", input = "der Rahm", skipped = false, round = 1, practiceMode = false }));
        Assert.True(reverse.Correct);
        Assert.Equal("die Sahne / der Rahm", reverse.Solution);
    }

    [Fact]
    public async Task Folders_and_languages_can_be_created_edited_and_deleted()
    {
        var client = await factory.CreateUserClientAsync();
        var lang = await Read<LanguageJson>(await client.PostAsJsonAsync("/api/languages", new { name = "Französisch" }));

        var created = await Read<FolderJson>(await client.PostAsJsonAsync("/api/folders",
            new { languageId = lang.Id, name = "Schule", text = "# Kapitel 1\ndie Schule = l'école\nder Lehrer = le professeur; l'enseignant" }));
        Assert.Equal(2, created.Chapters[0].Words.Count);

        var conflict = await client.DeleteAsync($"/api/languages/{lang.Id}");
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        var updated = await Read<FolderJson>(await client.PutAsJsonAsync($"/api/folders/{created.Id}",
            new { name = "Schule 2", text = "die Schule = l'école" }));
        Assert.Equal("Schule 2", updated.Name);

        var invalid = await client.PutAsJsonAsync($"/api/folders/{created.Id}", new { name = "Schule", text = "kein Gleichheitszeichen" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/folders/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/languages/{lang.Id}")).StatusCode);

        var renamed = await client.PutAsJsonAsync($"/api/languages/{(await SeedFolder(client)).Language.Id}", new { name = "English" });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
    }

    [Fact]
    public async Task Export_and_import_round_trip()
    {
        var client = await factory.CreateUserClientAsync();
        var (_, summary) = await SeedFolder(client);
        await client.PostAsJsonAsync($"/api/folders/{summary.Id}/quiz/answers",
            new { wordKey = "der Preis", direction = "toForeign", input = "prize", skipped = false, round = 1, practiceMode = false });

        var exportText = await (await client.GetAsync("/api/backup/export")).Content.ReadAsStringAsync();
        var json = JsonNode.Parse(exportText)!;
        Assert.Equal(1, json["version"]!.GetValue<int>());
        Assert.Equal("der Preis", json["languages"]![0]!["folders"]![0]!["weakWords"]![0]!.GetValue<string>());

        // Etwas ändern, dann wiederherstellen
        await client.PostAsJsonAsync("/api/languages", new { name = "Spanisch" });
        var import = await client.PostAsync("/api/backup/import", new StringContent(exportText, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, import.StatusCode);

        var languages = await Read<List<LanguageJson>>(await client.GetAsync("/api/languages"));
        var english = Assert.Single(languages);
        Assert.Equal(319, english.WordCount);
        Assert.Equal(1, english.WeakCount);

        var bad = await client.PostAsync("/api/backup/import", new StringContent("{\"version\":7,\"languages\":[]}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Photo_recognition_returns_lines_and_respects_daily_limit()
    {
        var client = await factory.CreateUserClientAsync();
        var status = await Read<StatusJson>(await client.GetAsync("/api/recognize/status"));
        Assert.True(status.Configured);
        Assert.Equal(2, status.RemainingToday);

        async Task<HttpResponseMessage> Send()
        {
            var image = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3]);
            image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            var form = new MultipartFormDataContent { { image, "photo", "seite.jpg" }, { new StringContent("Englisch"), "language" } };
            return await client.PostAsync("/api/recognize", form);
        }

        var result = await Read<RecognitionJson>(await Send());
        Assert.Equal(new[] { "# Obst", "die Birne = pear", "die Kasse = checkout; till" }, result.Lines);
        Assert.Equal(2, result.WordCount);
        Assert.Equal("test/model-a", result.Model);

        var request = factory.OpenRouter.Requests.Last();
        Assert.Equal("Bearer sk-or-test", request.Headers.Authorization!.ToString());
        Assert.Equal("Vokabeltrainer", request.Headers.GetValues("X-Title").Single());
        Assert.Contains("data:image/jpeg;base64,", factory.OpenRouter.RequestBodies.Last());
        Assert.Contains("Englisch-Deutsch-Vokabelbuch", factory.OpenRouter.RequestBodies.Last());

        await Read<RecognitionJson>(await Send());
        var limited = await Send();
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Contains("Tageslimit", (await limited.Content.ReadFromJsonAsync<ProblemJson>())!.Title);
    }

    [Fact]
    public async Task Unknown_api_route_is_404_and_health_is_ok()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/gibt-es-nicht")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }
}
