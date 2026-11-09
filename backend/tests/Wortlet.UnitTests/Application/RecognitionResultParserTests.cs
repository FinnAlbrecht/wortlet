using Wortlet.Application.Recognition;

namespace Wortlet.UnitTests.Application;

public class RecognitionResultParserTests
{
    private const string Json = """{"chapters":[{"name":"Obst","words":[{"de":"die Birne","en":["pear"]},{"de":"die Kasse","en":["checkout","till"]}]}]}""";

    [Fact]
    public void Reads_plain_json()
    {
        var chapters = RecognitionResultParser.Parse(Json);
        Assert.Equal("Obst", chapters[0].Name);
        Assert.Equal(2, chapters[0].Words.Count);
    }

    [Fact]
    public void Reads_json_inside_code_fence_with_text_around()
    {
        var content = "Hier sind die Wörter:\n```json\n" + Json + "\n```\nViel Erfolg!";
        var lines = RecognitionResultParser.ToLines(RecognitionResultParser.Parse(content));
        Assert.Equal(new[] { "# Obst", "die Birne = pear", "die Kasse = checkout; till" }, lines);
    }

    [Fact]
    public void Reads_json_with_prose_but_no_fence()
    {
        var chapters = RecognitionResultParser.Parse("Klar! " + Json + " Fertig.");
        Assert.Equal(2, chapters[0].Words.Count);
    }

    [Fact]
    public void Accepts_string_instead_of_list_and_bare_word_arrays()
    {
        var chapters = RecognitionResultParser.Parse("""[{"de":"der Apfel","en":"apple"}]""");
        Assert.Equal("apple", chapters[0].Words[0].Answers[0]);
    }

    [Fact]
    public void Lines_replace_separators_like_the_original()
    {
        var lines = RecognitionResultParser.ToLines([new RecognizedChapterDto("", [new RecognizedWordDto("a = b", ["x; y"])])]);
        Assert.Equal(new[] { "a - b = x, y" }, lines);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ich kann das Bild nicht lesen.")]
    [InlineData("{ kaputt")]
    public void Unreadable_answers_raise_recognition_error(string content)
    {
        var ex = Assert.Throws<RecognitionException>(() => RecognitionResultParser.Parse(content));
        Assert.Equal(RecognitionFailure.Unreadable, ex.Failure);
    }

    [Fact]
    public void Prompt_contains_language_and_json_shape()
    {
        var prompt = RecognitionPrompt.ForPhoto("Französisch");
        Assert.Contains("Französisch-Deutsch-Vokabelbuch", prompt);
        Assert.Contains("\"chapters\"", prompt);
    }
}
