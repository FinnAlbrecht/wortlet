using Wortlet.Domain.Common;
using Wortlet.Domain.Vocabulary;
using Wortlet.UnitTests.TestDoubles;

namespace Wortlet.UnitTests.Domain;

public class FolderTests
{
    private const string Text = "die Birne = pear\ndie Gurke = cucumber\nder Apfel = apple";

    [Fact]
    public void Wrong_answer_puts_word_on_weak_list()
    {
        var folder = TestData.Folder(Text);
        var wasWeak = folder.RecordAnswer("die Birne", correct: false, round: 1, practiceMode: false, TestData.Now);
        Assert.False(wasWeak);
        Assert.True(folder.IsWeak("die Birne"));
    }

    [Fact]
    public void Correct_in_normal_exam_does_not_remove_from_weak_list()
    {
        var folder = TestData.Folder(Text);
        folder.MarkWeak("die Birne", TestData.Now);
        folder.RecordAnswer("die Birne", correct: true, round: 1, practiceMode: false, TestData.Now);
        Assert.True(folder.IsWeak("die Birne"));
    }

    [Fact]
    public void Correct_in_practice_mode_first_round_removes_word()
    {
        var folder = TestData.Folder(Text);
        folder.MarkWeak("die Birne", TestData.Now);
        folder.RecordAnswer("die Birne", correct: true, round: 1, practiceMode: true, TestData.Now);
        Assert.False(folder.IsWeak("die Birne"));
    }

    [Fact]
    public void Correct_in_practice_mode_later_round_keeps_word()
    {
        var folder = TestData.Folder(Text);
        folder.MarkWeak("die Birne", TestData.Now);
        folder.RecordAnswer("die Birne", correct: true, round: 2, practiceMode: true, TestData.Now);
        Assert.True(folder.IsWeak("die Birne"));
    }

    [Fact]
    public void Override_removes_word_only_if_it_was_not_weak_before()
    {
        var folder = TestData.Folder(Text);
        var wasWeak = folder.RecordAnswer("die Birne", false, 1, false, TestData.Now);
        folder.OverrideAsCorrect("die Birne", 1, false, wasWeak);
        Assert.False(folder.IsWeak("die Birne"));

        folder.MarkWeak("die Gurke", TestData.Now);
        var wasWeak2 = folder.RecordAnswer("die Gurke", false, 1, false, TestData.Now);
        folder.OverrideAsCorrect("die Gurke", 1, false, wasWeak2);
        Assert.True(folder.IsWeak("die Gurke"));
    }

    [Fact]
    public void Override_in_practice_first_round_counts_as_known()
    {
        var folder = TestData.Folder(Text);
        folder.MarkWeak("die Gurke", TestData.Now);
        var wasWeak = folder.RecordAnswer("die Gurke", false, 1, true, TestData.Now);
        folder.OverrideAsCorrect("die Gurke", 1, true, wasWeak);
        Assert.False(folder.IsWeak("die Gurke"));
    }

    [Fact]
    public void Replacing_content_keeps_weak_words_that_still_exist()
    {
        var folder = TestData.Folder(Text);
        folder.MarkWeak("die Birne", TestData.Now);
        folder.MarkWeak("die Gurke", TestData.Now);
        folder.ReplaceContent(VocabularyText.Parse("die Birne = pear\ndie Zitrone = lemon").Chapters, TestData.Now);

        Assert.True(folder.IsWeak("die Birne"));
        Assert.False(folder.IsWeak("die Gurke"));
        Assert.Single(folder.WeakWords);
    }

    [Fact]
    public void Folder_needs_a_name_and_a_word()
    {
        Assert.Throws<DomainException>(() => TestData.Folder(Text, name: "  "));
        Assert.Throws<DomainException>(() => TestData.Folder("# nur Kapitel"));
    }

    [Fact]
    public void Unknown_word_cannot_be_marked()
    {
        var folder = TestData.Folder(Text);
        Assert.Throws<DomainException>(() => folder.MarkWeak("gibt es nicht", TestData.Now));
    }

    [Theory]
    [InlineData(10, 10, 6.0)]
    [InlineData(0, 10, 1.0)]
    [InlineData(5, 10, 3.5)]
    [InlineData(7, 10, 4.5)]
    public void Swiss_grade(int right, int total, double expected) =>
        Assert.Equal(expected, Grading.SwissGrade(right, total));
}
