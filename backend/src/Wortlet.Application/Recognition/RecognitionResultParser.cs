using System.Text.Json;
using System.Text.RegularExpressions;

namespace Wortlet.Application.Recognition;

/// <summary>
/// Liest die Antwort des Modells robust aus: reines JSON, JSON in ```json … ```, oder JSON mit Text davor/danach.
/// Wandelt das Ergebnis wie im Original in Zeilen „Deutsch = Fremdsprache“ um.
/// </summary>
public static partial class RecognitionResultParser
{
    [GeneratedRegex(@"```(?:json|JSON)?\s*([\s\S]*?)```")]
    private static partial Regex Fence();

    public static IReadOnlyList<RecognizedChapterDto> Parse(string? content)
    {
        var json = ExtractJson(content)
            ?? throw new RecognitionException(RecognitionFailure.Unreadable, "Auf dem Foto wurden keine Wörter gefunden. Ist die Seite scharf und gut beleuchtet?");

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException)
        {
            throw new RecognitionException(RecognitionFailure.Unreadable, "Die Antwort der KI war unvollständig. Versuche es mit diesem Foto nochmals.");
        }

        using (doc)
        {
            var root = doc.RootElement;
            var chapters = new List<RecognizedChapterDto>();
            if (root.ValueKind == JsonValueKind.Object && TryGet(root, "chapters", out var chs) && chs.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in chs.EnumerateArray())
                {
                    if (c.ValueKind != JsonValueKind.Object) continue;
                    var name = TryGet(c, "name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() ?? "" : "";
                    var words = TryGet(c, "words", out var ws) ? ReadWords(ws) : [];
                    chapters.Add(new RecognizedChapterDto(name.TrimStart('#').Trim(), words));
                }
            }
            else if (root.ValueKind == JsonValueKind.Object && TryGet(root, "words", out var ws))
            {
                chapters.Add(new RecognizedChapterDto("", ReadWords(ws)));
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                chapters.Add(new RecognizedChapterDto("", ReadWords(root)));
            }
            return chapters;
        }
    }

    /// <summary>Wie im Original: „# Kapitel“ und „Deutsch = A; B“. „=“ im Deutschen und „;“ in Antworten werden ersetzt.</summary>
    public static IReadOnlyList<string> ToLines(IEnumerable<RecognizedChapterDto> chapters)
    {
        var lines = new List<string>();
        foreach (var c in chapters)
        {
            var name = c.Name.TrimStart('#').Trim();
            if (name.Length > 0) lines.Add("# " + name);
            foreach (var w in c.Words)
            {
                var de = w.De.Replace("=", "-").Trim();
                var answers = w.Answers.Select(a => a.Replace(";", ",").Replace("|", "/").Trim()).Where(a => a.Length > 0).ToList();
                if (de.Length > 0 && answers.Count > 0)
                    lines.Add(de + " = " + string.Join("; ", answers));
            }
        }
        return lines;
    }

    internal static string? ExtractJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = content.Trim();
        var fence = Fence().Match(text);
        if (fence.Success) text = fence.Groups[1].Value.Trim();

        var firstObj = text.IndexOf('{');
        var firstArr = text.IndexOf('[');
        if (firstObj < 0 && firstArr < 0) return null;
        var useArray = firstArr >= 0 && (firstObj < 0 || firstArr < firstObj);
        var start = useArray ? firstArr : firstObj;
        var end = text.LastIndexOf(useArray ? ']' : '}');
        return end > start ? text[start..(end + 1)] : null;
    }

    private static List<RecognizedWordDto> ReadWords(JsonElement ws)
    {
        var list = new List<RecognizedWordDto>();
        if (ws.ValueKind != JsonValueKind.Array) return list;
        foreach (var w in ws.EnumerateArray())
        {
            if (w.ValueKind != JsonValueKind.Object) continue;
            var de = TryGet(w, "de", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() ?? "" : "";
            var answers = new List<string>();
            foreach (var key in new[] { "en", "answers", "foreign" })
            {
                if (!TryGet(w, key, out var a)) continue;
                if (a.ValueKind == JsonValueKind.String) answers.Add(a.GetString() ?? "");
                else if (a.ValueKind == JsonValueKind.Array)
                    answers.AddRange(a.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString() ?? ""));
                break;
            }
            answers = answers.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            if (de.Trim().Length > 0 && answers.Count > 0)
                list.Add(new RecognizedWordDto(de.Trim(), answers));
        }
        return list;
    }

    private static bool TryGet(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }
        value = default;
        return false;
    }
}
