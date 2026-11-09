using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Wortlet.IntegrationTests;

/// <summary>Gruppen suchen, öffentlich beitreten, Anfragen stellen und Sichtbarkeit ändern.</summary>
public class GroupSearchTests(WortletFactory factory) : IClassFixture<WortletFactory>
{
    private static async Task<JsonNode> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {text}");
        return JsonNode.Parse(text)!;
    }

    private static async Task<JsonArray> Search(HttpClient client, string q) =>
        (await Json(await client.GetAsync($"/api/groups/search?q={Uri.EscapeDataString(q)}"))).AsArray();

    private static string S(JsonNode? node, string key) => node![key]!.GetValue<string>();

    [Fact]
    public async Task Search_finds_public_and_on_request_groups_but_never_private_ones()
    {
        var leader = await factory.CreateUserClientAsync();
        var other = await factory.CreateUserClientAsync();
        var tag = Guid.NewGuid().ToString("N")[..6];
        await Json(await leader.PutAsJsonAsync("/api/profile", new { displayName = "Frau Muster", avatarIcon = "🦊", avatarColor = "#2a3c9c" }));

        await Json(await leader.PostAsJsonAsync("/api/groups", new { name = $"Französisch {tag} offen", visibility = "public" }));
        await Json(await leader.PostAsJsonAsync("/api/groups", new { name = $"Französisch {tag} Anfrage", visibility = "onRequest" }));
        var privat = await Json(await leader.PostAsJsonAsync("/api/groups", new { name = $"Französisch {tag} geheim" }));
        Assert.Equal("private", S(privat, "visibility"));

        // Gross-/Kleinschreibung und Umlaute sind egal
        var hits = await Search(other, $"franzosisch {tag.ToUpperInvariant()}");
        Assert.Equal(2, hits.Count);
        Assert.DoesNotContain(hits, h => S(h, "name").Contains("geheim"));
        Assert.All(hits, h => Assert.Equal("Frau Muster", S(h, "leaderName")));
        Assert.All(hits, h => Assert.Equal("none", S(h, "status")));
        Assert.DoesNotContain("joinCode", hits.ToJsonString()); // der Code wird nie verraten

        Assert.Equal(HttpStatusCode.BadRequest, (await other.GetAsync("/api/groups/search?q=a")).StatusCode);

        // Private Gruppe lässt sich auch mit der ID nicht direkt betreten
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/groups/{S(privat, "id")}/join", null)).StatusCode);
    }

    [Fact]
    public async Task Public_groups_are_joined_directly()
    {
        var leader = await factory.CreateUserClientAsync();
        var member = await factory.CreateUserClientAsync();
        var group = await Json(await leader.PostAsJsonAsync("/api/groups", new { name = "Offene Lerngruppe", visibility = "public" }));

        var result = await Json(await member.PostAsync($"/api/groups/{S(group, "id")}/join", null));
        Assert.Equal("joined", S(result, "status"));
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/api/groups/{S(group, "id")}")).StatusCode);
    }

    [Fact]
    public async Task On_request_groups_wait_for_the_leader()
    {
        var leader = await factory.CreateUserClientAsync();
        var anna = await factory.CreateUserClientAsync();
        var ben = await factory.CreateUserClientAsync();
        await Json(await anna.PutAsJsonAsync("/api/profile", new { displayName = "Anna", avatarIcon = "🐼", avatarColor = "#be185d" }));
        var group = await Json(await leader.PostAsJsonAsync("/api/groups", new { name = "Klasse mit Anfrage", visibility = "onRequest" }));
        var id = S(group, "id");

        Assert.Equal("requested", S(await Json(await anna.PostAsync($"/api/groups/{id}/join", null)), "status"));
        Assert.Equal("requested", S(await Json(await ben.PostAsync($"/api/groups/{id}/join", null)), "status"));

        // Noch kein Zugriff, aber die Anfrage ist sichtbar
        Assert.Equal(HttpStatusCode.NotFound, (await anna.GetAsync($"/api/groups/{id}")).StatusCode);
        var mine = (await Json(await anna.GetAsync("/api/groups/requests"))).AsArray();
        Assert.Equal("Klasse mit Anfrage", S(Assert.Single(mine), "name"));
        Assert.Equal("requested", S((await Search(anna, "Klasse mit Anfrage")).First(h => S(h, "id") == id), "status"));

        // Leitung sieht die Anfragen (mit Namen, ohne E-Mail) – Mitglieder nicht
        var detail = await Json(await leader.GetAsync($"/api/groups/{id}"));
        var requests = detail["joinRequests"]!.AsArray();
        Assert.Equal(2, requests.Count);
        Assert.Equal("Anna", S(requests[0]!["profile"], "displayName"));
        Assert.DoesNotContain("@", requests.ToJsonString());
        var summary = (await Json(await leader.GetAsync("/api/groups"))).AsArray().First(g => S(g, "id") == id)!;
        Assert.Equal(2, summary["pendingRequests"]!.GetValue<int>());

        var annaId = S(requests[0]!["profile"], "userId");
        var benId = S(requests[1]!["profile"], "userId");
        Assert.Equal(HttpStatusCode.NotFound, (await ben.PostAsync($"/api/groups/{id}/requests/{annaId}/accept", null)).StatusCode);

        var accepted = await Json(await leader.PostAsync($"/api/groups/{id}/requests/{annaId}/accept", null));
        Assert.Equal(2, accepted["ranking"]!.AsArray().Count);
        var declined = await Json(await leader.DeleteAsync($"/api/groups/{id}/requests/{benId}"));
        Assert.Empty(declined["joinRequests"]!.AsArray());

        var annaDetail = await Json(await anna.GetAsync($"/api/groups/{id}"));
        Assert.Empty(annaDetail["joinRequests"]!.AsArray()); // nur die Leitung sieht Anfragen
        Assert.Empty((await Json(await ben.GetAsync("/api/groups/requests"))).AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync($"/api/groups/{id}")).StatusCode);
    }

    [Fact]
    public async Task Requests_can_be_withdrawn_and_visibility_changed_by_the_leader_only()
    {
        var leader = await factory.CreateUserClientAsync();
        var anna = await factory.CreateUserClientAsync();
        var group = await Json(await leader.PostAsJsonAsync("/api/groups", new { name = "Wechselnde Gruppe", visibility = "onRequest" }));
        var id = S(group, "id");

        await Json(await anna.PostAsync($"/api/groups/{id}/join", null));
        Assert.Equal(HttpStatusCode.NoContent, (await anna.DeleteAsync($"/api/groups/{id}/request")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anna.DeleteAsync($"/api/groups/{id}/request")).StatusCode);

        // Wird die Gruppe öffentlich, werden offene Anfragen angenommen
        await Json(await anna.PostAsync($"/api/groups/{id}/join", null));
        var opened = await Json(await leader.PutAsJsonAsync($"/api/groups/{id}/visibility", new { visibility = "public" }));
        Assert.Equal("public", S(opened, "visibility"));
        Assert.Equal(2, opened["ranking"]!.AsArray().Count);

        Assert.Equal(HttpStatusCode.BadRequest, (await anna.PutAsJsonAsync($"/api/groups/{id}/visibility", new { visibility = "private" })).StatusCode);

        // Privat: verschwindet aus der Suche
        await Json(await leader.PutAsJsonAsync($"/api/groups/{id}/visibility", new { visibility = "private" }));
        Assert.Empty(await Search(anna, "Wechselnde Gruppe"));
    }
}
