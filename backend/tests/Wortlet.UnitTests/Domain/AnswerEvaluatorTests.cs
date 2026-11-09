using Wortlet.Domain.Vocabulary;
using Wortlet.UnitTests.TestDoubles;

namespace Wortlet.UnitTests.Domain;

public class AnswerEvaluatorTests
{
    private const string Text = """
        # Obst
        das Obst, die Frucht = fruit
        die Sahne = cream
        der Rahm = cream
        ausgeben (Geld) = to spend
        die Kasse = checkout; cash desk; till
        der Blumenladen = florist's
        hart gekocht = hard-boiled
        Was kostet …? = how much is ...?
        der Rosé = rosé; rose
        """;

    [Theory]
    [InlineData("fruit", true)]
    [InlineData("FRUIT", true)]
    [InlineData("  fruit  ", true)]
    [InlineData("fruits", false)]
    [InlineData("the fruit", false)]
    public void Exact_answer_only_case_is_ignored(string input, bool expected) =>
        Assert.Equal(expected, AnswerEvaluator.IsMatch(input, ["fruit"]));

    [Fact]
    public void Apostrophe_variants_and_ellipsis_are_equal()
    {
        Assert.True(AnswerEvaluator.IsMatch("florist’s", ["florist's"]));
        Assert.True(AnswerEvaluator.IsMatch("how much is …?", ["how much is ...?"]));
        Assert.False(AnswerEvaluator.IsMatch("florists", ["florist's"]));
    }

    [Fact]
    public void Verbs_need_to_and_hyphen_must_match()
    {
        Assert.False(AnswerEvaluator.IsMatch("spend", ["to spend"]));
        Assert.True(AnswerEvaluator.IsMatch("to spend", ["to spend"]));
        Assert.False(AnswerEvaluator.IsMatch("hard boiled", ["hard-boiled"]));
    }

    [Fact]
    public void Any_alternative_counts()
    {
        Assert.True(AnswerEvaluator.IsMatch("till", ["checkout", "cash desk", "till"]));
        Assert.True(AnswerEvaluator.IsMatch("Cash Desk", ["checkout", "cash desk", "till"]));
    }

    [Fact]
    public void German_variants_whole_part_and_without_parentheses()
    {
        var v = AnswerEvaluator.GermanVariants("das Obst, die Frucht");
        Assert.Contains("das Obst, die Frucht", v);
        Assert.Contains("das Obst", v);
        Assert.Contains("die Frucht", v);

        var p = AnswerEvaluator.GermanVariants("ausgeben (Geld)");
        Assert.Contains("ausgeben (Geld)", p);
        Assert.Contains("ausgeben", p);
    }

    [Fact]
    public void Reverse_direction_requires_article_and_accepts_part()
    {
        var folder = TestData.Folder(Text);
        var words = folder.AllWords.ToList();
        var fruit = folder.GetWord("das Obst, die Frucht");

        Assert.True(AnswerEvaluator.Evaluate(fruit, words, QuizDirection.ToGerman, "das Obst", false).IsCorrect);
        Assert.True(AnswerEvaluator.Evaluate(fruit, words, QuizDirection.ToGerman, "Die Frucht", false).IsCorrect);
        Assert.False(AnswerEvaluator.Evaluate(fruit, words, QuizDirection.ToGerman, "Obst", false).IsCorrect);
    }

    [Fact]
    public void Reverse_direction_accepts_all_german_meanings()
    {
        var folder = TestData.Folder(Text);
        var words = folder.AllWords.ToList();
        var sahne = folder.GetWord("die Sahne");

        var result = AnswerEvaluator.Evaluate(sahne, words, QuizDirection.ToGerman, "der Rahm", false);
        Assert.True(result.IsCorrect);
        Assert.Equal("die Sahne / der Rahm", result.Solution);
        Assert.False(AnswerEvaluator.Evaluate(sahne, words, QuizDirection.ToGerman, "Sahne", false).IsCorrect);
    }

    [Fact]
    public void Reverse_direction_parentheses_are_optional()
    {
        var folder = TestData.Folder(Text);
        var spend = folder.GetWord("ausgeben (Geld)");
        Assert.True(AnswerEvaluator.Evaluate(spend, folder.AllWords.ToList(), QuizDirection.ToGerman, "ausgeben", false).IsCorrect);
    }

    [Fact]
    public void Skipped_is_never_correct()
    {
        var folder = TestData.Folder(Text);
        var w = folder.GetWord("die Sahne");
        Assert.False(AnswerEvaluator.Evaluate(w, folder.AllWords.ToList(), QuizDirection.ToForeign, "cream", true).IsCorrect);
    }

    [Fact]
    public void Solution_hides_keyboard_variants()
    {
        Assert.Equal("rosé", AnswerEvaluator.Solution(["rosé", "rose"]));
        Assert.Equal("checkout / cash desk / till", AnswerEvaluator.Solution(["checkout", "cash desk", "till"]));
        Assert.Equal("candy", AnswerEvaluator.Solution(["candy"]));
    }

    [Fact]
    public void Base_key_strips_duplicate_suffix()
    {
        Assert.Equal("die Bank", AnswerEvaluator.BaseKey("die Bank (2)"));
        Assert.Equal("ausgeben (Geld)", AnswerEvaluator.BaseKey("ausgeben (Geld)"));
    }
}
