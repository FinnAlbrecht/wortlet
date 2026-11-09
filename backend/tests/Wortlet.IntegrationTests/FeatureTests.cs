using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Wortlet.IntegrationTests;

/// <summary>Profil, Statistik, Sets und Gruppen über die echte API.</summary>
public class FeatureTests(WortletFactory factory) : IClassFixture<WortletFactory>
{
    // Kleinstes gültiges PNG (1×1 Pixel)
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static async Task<JsonNode> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {text}");
        return JsonNode.Parse(text)!;
    }

    private static async Task<string> Title(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!["title"]!.GetValue<string>();

    private static async Task<(string LanguageId, string FolderId)> Seed(HttpClient client)
    {
        var languages = (await Json(await client.GetAsync("/api/languages"))).AsArray();
        var languageId = languages[0]!["id"]!.GetValue<string>();
        var folders = (await Json(await client.GetAsync($"/api/languages/{languageId}/folders"))).AsArray();
        return (languageId, folders[0]!["id"]!.GetValue<string>());
    }

    private static Task<HttpResponseMessage> Answer(HttpClient client, string folderId, string key, string input) =>
        client.PostAsJsonAsync($"/api/folders/{folderId}/quiz/answers",
            new { wordKey = key, direction = "toForeign", input, skipped = false, round = 1, practiceMode = false });

    private static async Task<HttpResponseMessage> UploadAvatar(HttpClient client, byte[] data)
    {
        using var form = new MultipartFormDataContent { { new ByteArrayContent(data), "image", "a.png" } };
        return await client.PutAsync("/api/profile/avatar", form);
    }

    [Fact]
    public async Task Profile_has_defaults_and_can_be_changed()
    {
        var email = $"anna.muster-{Guid.NewGuid():N}"[..20] + "@example.test";
        var client = await factory.CreateUserClientAsync(email);

        var profile = await Json(await client.GetAsync("/api/profile"));
        Assert.Equal(email.Split('@')[0], profile["displayName"]!.GetValue<string>());
        Assert.False(profile["hasImage"]!.GetValue<bool>());

        var options = await Json(await client.GetAsync("/api/profile/options"));
        var icon = options["icons"]![2]!.GetValue<string>();
        var color = options["colors"]![1]!.GetValue<string>();
        var updated = await Json(await client.PutAsJsonAsync("/api/profile", new { displayName = "  Anna  ", avatarIcon = icon, avatarColor = color }));
        Assert.Equal("Anna", updated["displayName"]!.GetValue<string>());
        Assert.Equal(icon, updated["avatarIcon"]!.GetValue<string>());

        var badIcon = await client.PutAsJsonAsync("/api/profile", new { displayName = "Anna", avatarIcon = "💩💩", avatarColor = color });
        Assert.Equal(HttpStatusCode.BadRequest, badIcon.StatusCode);
    }

    [Fact]
    public async Task Avatar_image_is_validated_and_only_visible_to_group_members()
    {
        var anna = await factory.CreateUserClientAsync();
        var ben = await factory.CreateUserClientAsync();
        var annaId = (await Json(await anna.GetAsync("/api/profile")))["userId"]!.GetValue<string>();

        var notAnImage = await UploadAvatar(anna, "<script>alert(1)</script>"u8.ToArray());
        Assert.Equal(HttpStatusCode.BadRequest, notAnImage.StatusCode);

        var uploaded = await Json(await UploadAvatar(anna, Png));
        Assert.True(uploaded["hasImage"]!.GetValue<bool>());

        var own = await anna.GetAsync($"/api/profile/{annaId}/avatar");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal("image/png", own.Content.Headers.ContentType!.MediaType);

        // Fremde sehen das Bild nicht …
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync($"/api/profile/{annaId}/avatar")).StatusCode);

        // … Mitglieder derselben Gruppe schon.
        var group = await Json(await anna.PostAsJsonAsync("/api/groups", new { name = "Avatare" }));
        await Json(await ben.PostAsJsonAsync("/api/groups/join", new { code = group["joinCode"]!.GetValue<string>() }));
        Assert.Equal(HttpStatusCode.OK, (await ben.GetAsync($"/api/profile/{annaId}/avatar")).StatusCode);

        var removed = await Json(await anna.DeleteAsync("/api/profile/avatar"));
        Assert.False(removed["hasImage"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Answers_and_results_show_up_in_statistics()
    {
        var client = await factory.CreateUserClientAsync();
        var (languageId, folderId) = await Seed(client);

        await Json(await Answer(client, folderId, "die Sahne", "creme"));   // falsch
        await Json(await Answer(client, folderId, "die Sahne", "cream"));   // richtig
        await Json(await Answer(client, folderId, "die Birne", "pear"));    // richtig

        var saved = await Json(await client.PostAsJsonAsync("/api/stats/results",
            new { folderId, direction = "de", practiceMode = false, wordCount = 4, firstRoundCorrect = 3 }));
        Assert.Equal(5, saved["grade"]!.GetValue<double>()); // 3/4 → 4.75 → auf halbe Noten gerundet
        Assert.Equal("Einkaufen & Ernährung", saved["title"]!.GetValue<string>());

        var overview = await Json(await client.GetAsync("/api/stats/overview"));
        Assert.Equal(3, overview["totalAnswers"]!.GetValue<int>());
        Assert.Equal(2, overview["correctAnswers"]!.GetValue<int>());
        Assert.Equal(2, overview["wordsPracticed"]!.GetValue<int>());
        Assert.Equal(1, overview["quizzes"]!.GetValue<int>());
        Assert.Equal(3, overview["todayAnswers"]!.GetValue<int>());
        Assert.Equal(1, overview["currentStreak"]!.GetValue<int>());
        Assert.Equal(28, overview["lastDays"]!.AsArray().Count);

        var hardest = (await Json(await client.GetAsync($"/api/stats/hardest?languageId={languageId}"))).AsArray();
        var hard = Assert.Single(hardest);
        Assert.Equal("die Sahne", hard!["german"]!.GetValue<string>());
        Assert.Equal(1, hard["wrong"]!.GetValue<int>());

        var results = (await Json(await client.GetAsync("/api/stats/results"))).AsArray();
        Assert.Single(results);

        var invalid = await client.PostAsJsonAsync("/api/stats/results",
            new { folderId, direction = "de", practiceMode = false, wordCount = 2, firstRoundCorrect = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Override_corrects_the_statistics()
    {
        var client = await factory.CreateUserClientAsync();
        var (_, folderId) = await Seed(client);
        var wrong = await Json(await Answer(client, folderId, "die Birne", "peer"));
        await Json(await client.PostAsJsonAsync($"/api/folders/{folderId}/quiz/override",
            new { wordKey = "die Birne", round = 1, practiceMode = false, wasWeakBefore = wrong["wasWeakBefore"]!.GetValue<bool>() }));

        var overview = await Json(await client.GetAsync("/api/stats/overview"));
        Assert.Equal(1, overview["correctAnswers"]!.GetValue<int>());
        Assert.Empty((await Json(await client.GetAsync("/api/stats/hardest"))).AsArray());
    }

    [Fact]
    public async Task Sets_reference_words_across_folders_and_follow_changes()
    {
        var client = await factory.CreateUserClientAsync();
        var (languageId, seedId) = await Seed(client);
        var own = await Json(await client.PostAsJsonAsync("/api/folders",
            new { languageId, name = "Eigene", text = "der Hund = dog\ndie Katze = cat" }));
        var ownId = own["id"]!.GetValue<string>();

        var set = await Json(await client.PostAsJsonAsync("/api/sets", new
        {
            languageId,
            name = "Prüfung Freitag",
            items = new object[]
            {
                new { folderId = seedId, wordKey = "die Birne" },
                new { folderId = ownId, wordKey = "der Hund" },
                new { folderId = ownId, wordKey = "der Hund" }, // doppelt → nur einmal
            },
        }));
        var setId = set["id"]!.GetValue<string>();
        var words = set["words"]!.AsArray();
        Assert.Equal(2, words.Count);
        Assert.Equal(ownId, words[1]!["folderId"]!.GetValue<string>());

        var listed = (await Json(await client.GetAsync($"/api/languages/{languageId}/sets"))).AsArray();
        Assert.Equal("Prüfung Freitag", Assert.Single(listed)!["name"]!.GetValue<string>());

        var unknown = await client.PostAsJsonAsync("/api/sets",
            new { languageId, name = "X", items = new[] { new { folderId = ownId, wordKey = "gibt es nicht" } } });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        // Korrektur im Ordner wirkt sofort im Set
        await Json(await client.PutAsJsonAsync($"/api/folders/{ownId}", new { name = "Eigene", text = "der Hund = dog; hound\ndie Katze = cat" }));
        var after = await Json(await client.GetAsync($"/api/sets/{setId}"));
        Assert.Equal("dog / hound", after["words"]![1]!["solution"]!.GetValue<string>());

        // Ergebnis eines Sets zählt im Verlauf
        await Json(await client.PostAsJsonAsync("/api/stats/results", new { setId, direction = "mix", practiceMode = false, wordCount = 2, firstRoundCorrect = 2 }));

        // Ordner gelöscht → seine Wörter fallen aus dem Set, das Ergebnis bleibt
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/folders/{ownId}")).StatusCode);
        var shrunk = await Json(await client.GetAsync($"/api/sets/{setId}"));
        Assert.Single(shrunk["words"]!.AsArray());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/sets/{setId}")).StatusCode);
        Assert.Single((await Json(await client.GetAsync("/api/stats/results"))).AsArray());

        // Fremde Sets sind unsichtbar
        var other = await factory.CreateUserClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/sets/{setId}")).StatusCode);
    }

    [Fact]
    public async Task Groups_join_ranking_sharing_and_copies()
    {
        var leader = await factory.CreateUserClientAsync();
        var member = await factory.CreateUserClientAsync();
        var stranger = await factory.CreateUserClientAsync();
        var (leaderLanguage, leaderFolder) = await Seed(leader);
        var shared = await Json(await leader.PostAsJsonAsync("/api/folders",
            new { languageId = leaderLanguage, name = "Klassenwörter", text = "# Woche 1\nder Hund = dog\ndie Katze = cat" }));
        var sharedId = shared["id"]!.GetValue<string>();

        var group = await Json(await leader.PostAsJsonAsync("/api/groups", new { name = "Klasse 3b" }));
        var groupId = group["id"]!.GetValue<string>();
        var code = group["joinCode"]!.GetValue<string>();
        Assert.Equal(8, code.Length);
        Assert.True(group["isLeader"]!.GetValue<bool>());

        // Code mit Kleinbuchstaben und Bindestrich eintippen geht auch
        var typed = code[..4].ToLowerInvariant() + "-" + code[4..];
        var joined = await Json(await member.PostAsJsonAsync("/api/groups/join", new { code = typed }));
        Assert.False(joined["isLeader"]!.GetValue<bool>());
        Assert.Equal(2, joined["ranking"]!.AsArray().Count);

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync("/api/groups/join", new { code = "ZZZZZZZZ" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/groups/{groupId}")).StatusCode);

        // Rangliste: wer diese Woche mehr geübt hat, steht oben. Keine E-Mail-Adressen.
        var (_, memberFolder) = await Seed(member);
        await Json(await Answer(member, memberFolder, "die Birne", "pear"));
        await Json(await Answer(member, memberFolder, "die Sahne", "cream"));
        await Json(await Answer(leader, leaderFolder, "die Birne", "pear"));
        var detail = await Json(await member.GetAsync($"/api/groups/{groupId}"));
        var ranking = detail["ranking"]!.AsArray();
        Assert.True(ranking[0]!["isMe"]!.GetValue<bool>());
        Assert.Equal(2, ranking[0]!["weekAnswers"]!.GetValue<int>());
        Assert.Equal(1, ranking[1]!["weekAnswers"]!.GetValue<int>());
        Assert.DoesNotContain("@", detail.ToJsonString());

        // Nur die Leitung teilt
        Assert.Equal(HttpStatusCode.BadRequest, (await member.PostAsJsonAsync($"/api/groups/{groupId}/shares", new { folderId = memberFolder })).StatusCode);
        var withShare = await Json(await leader.PostAsJsonAsync($"/api/groups/{groupId}/shares", new { folderId = sharedId }));
        var share = Assert.Single(withShare["shares"]!.AsArray())!;
        Assert.Equal("Klassenwörter", share["name"]!.GetValue<string>());
        Assert.Equal(2, share["wordCount"]!.GetValue<int>());
        Assert.True(share["isMine"]!.GetValue<bool>());
        var shareId = share["id"]!.GetValue<string>();

        // Mitglied übernimmt eine Kopie in die eigene Sprache „Englisch“
        var copy = await Json(await member.PostAsJsonAsync($"/api/groups/{groupId}/shares/{shareId}/copy", new { }));
        Assert.False(copy["updated"]!.GetValue<bool>());
        var copyId = copy["folderId"]!.GetValue<string>();
        var copied = await Json(await member.GetAsync($"/api/folders/{copyId}"));
        Assert.Equal("Klassenwörter", copied["name"]!.GetValue<string>());
        Assert.Equal("Englisch", copied["languageName"]!.GetValue<string>());
        Assert.Equal("Aus der Gruppe „Klasse 3b“", copied["description"]!.GetValue<string>());

        // Leitung ergänzt ein Wort → Mitglied aktualisiert seine Kopie (keine zweite Kopie)
        await Json(await leader.PutAsJsonAsync($"/api/folders/{sharedId}", new { name = "Klassenwörter", text = "der Hund = dog\ndie Katze = cat\ndie Maus = mouse" }));
        var again = await Json(await member.PostAsJsonAsync($"/api/groups/{groupId}/shares/{shareId}/copy", new { }));
        Assert.True(again["updated"]!.GetValue<bool>());
        Assert.Equal(copyId, again["folderId"]!.GetValue<string>());
        Assert.Equal(3, (await Json(await member.GetAsync($"/api/folders/{copyId}")))["chapters"]![0]!["words"]!.AsArray().Count);

        var memberView = await Json(await member.GetAsync($"/api/groups/{groupId}"));
        Assert.Equal(copyId, memberView["shares"]![0]!["myCopyFolderId"]!.GetValue<string>());
        Assert.False(memberView["shares"]![0]!["isMine"]!.GetValue<bool>());

        // Leitung kann nicht austreten, Mitglied schon
        Assert.Equal(HttpStatusCode.BadRequest, (await leader.PostAsync($"/api/groups/{groupId}/leave", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/api/groups/{groupId}/leave", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/groups/{groupId}")).StatusCode);
        Assert.Empty((await Json(await member.GetAsync("/api/groups"))).AsArray());

        // Die eigene Kopie bleibt dem ehemaligen Mitglied
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/api/folders/{copyId}")).StatusCode);

        // Neuer Code: der alte gilt nicht mehr
        var renewed = await Json(await leader.PostAsync($"/api/groups/{groupId}/code", null));
        Assert.NotEqual(code, renewed["joinCode"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.NotFound, (await member.PostAsJsonAsync("/api/groups/join", new { code })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await leader.DeleteAsync($"/api/groups/{groupId}")).StatusCode);
    }

    [Fact]
    public async Task Shared_sets_become_a_folder_with_one_chapter_per_source_folder()
    {
        var leader = await factory.CreateUserClientAsync();
        var member = await factory.CreateUserClientAsync();
        var (languageId, seedId) = await Seed(leader);
        var set = await Json(await leader.PostAsJsonAsync("/api/sets", new
        {
            languageId,
            name = "Test Freitag",
            items = new[] { new { folderId = seedId, wordKey = "die Birne" }, new { folderId = seedId, wordKey = "die Sahne" } },
        }));
        var group = await Json(await leader.PostAsJsonAsync("/api/groups", new { name = "Set-Gruppe" }));
        var groupId = group["id"]!.GetValue<string>();
        await Json(await member.PostAsJsonAsync("/api/groups/join", new { code = group["joinCode"]!.GetValue<string>() }));
        var withShare = await Json(await leader.PostAsJsonAsync($"/api/groups/{groupId}/shares", new { setId = set["id"]!.GetValue<string>() }));
        var share = withShare["shares"]![0]!;
        Assert.Equal("set", share["kind"]!.GetValue<string>());

        var copy = await Json(await member.PostAsJsonAsync($"/api/groups/{groupId}/shares/{share["id"]!.GetValue<string>()}/copy", new { }));
        var folder = await Json(await member.GetAsync($"/api/folders/{copy["folderId"]!.GetValue<string>()}"));
        Assert.Equal("Test Freitag", folder["name"]!.GetValue<string>());
        var chapter = Assert.Single(folder["chapters"]!.AsArray())!;
        Assert.Equal("Einkaufen & Ernährung", chapter["name"]!.GetValue<string>());
        Assert.Equal(2, chapter["words"]!.AsArray().Count);
    }
}
