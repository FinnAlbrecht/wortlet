using Wortlet.Domain.Common;
using Wortlet.Domain.Groups;
using Wortlet.Domain.Profiles;
using Wortlet.Domain.Sets;
using Wortlet.Domain.Statistics;
using Wortlet.UnitTests.TestDoubles;

namespace Wortlet.UnitTests.Domain;

public class StreakTests
{
    private static readonly DateOnly Today = new(2026, 11, 9); // Montag

    private static DateOnly D(int daysAgo) => Today.AddDays(-daysAgo);

    [Fact]
    public void No_days_means_no_streak() => Assert.Equal((0, 0), Streaks.Compute([], Today));

    [Fact]
    public void Streak_counts_back_from_today()
    {
        Assert.Equal((3, 3), Streaks.Compute([D(0), D(1), D(2)], Today));
    }

    [Fact]
    public void Streak_is_not_lost_before_today_is_practiced()
    {
        Assert.Equal((2, 2), Streaks.Compute([D(1), D(2)], Today));
    }

    [Fact]
    public void A_gap_ends_the_current_streak_but_longest_is_kept()
    {
        Assert.Equal((1, 4), Streaks.Compute([D(0), D(5), D(6), D(7), D(8), D(8)], Today));
        Assert.Equal((0, 2), Streaks.Compute([D(2), D(3)], Today));
    }

    [Theory]
    [InlineData("2026-11-09", "2026-11-09")] // Montag
    [InlineData("2026-11-12", "2026-11-09")] // Donnerstag
    [InlineData("2026-11-15", "2026-11-09")] // Sonntag gehört zur Woche davor
    public void Week_starts_on_monday(string day, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), Streaks.WeekStart(DateOnly.Parse(day)));

    [Fact]
    public void Quiz_result_gets_swiss_grade_and_checks_counts()
    {
        var r = QuizResult.Create(TestData.Owner, Guid.NewGuid(), null, "Test", "de", false, 10, 7, TestData.Now);
        Assert.Equal(4.5, r.Grade);
        Assert.Throws<DomainException>(() => QuizResult.Create(TestData.Owner, Guid.NewGuid(), null, "Test", "de", false, 10, 11, TestData.Now));
        Assert.Throws<DomainException>(() => QuizResult.Create(TestData.Owner, null, null, "Test", "de", false, 10, 5, TestData.Now));
        Assert.Throws<DomainException>(() => QuizResult.Create(TestData.Owner, Guid.NewGuid(), null, "Test", "xx", false, 10, 5, TestData.Now));
    }

    [Fact]
    public void Overturn_turns_the_last_wrong_answer_into_a_right_one()
    {
        var stat = WordStat.Start(TestData.Owner, Guid.NewGuid(), "die Birne");
        stat.Record(false, TestData.Now);
        stat.Overturn();
        Assert.Equal((1, 0, true), (stat.Attempts, stat.Wrong, stat.LastCorrect));

        var day = PracticeDay.Start(TestData.Owner, new DateOnly(2026, 11, 9));
        day.Record(false);
        day.Overturn();
        day.Overturn(); // nie mehr richtige als Antworten
        Assert.Equal((1, 1), (day.Answers, day.Correct));
    }
}

public class ProfileTests
{
    [Fact]
    public void Default_profile_uses_the_email_name_and_a_valid_avatar()
    {
        var p = UserProfile.CreateDefault(Guid.NewGuid(), "anna.muster@example.test", TestData.Now);
        Assert.Equal("anna.muster", p.DisplayName);
        Assert.Contains(p.AvatarIcon, UserProfile.Icons);
        Assert.Contains(p.AvatarColor, UserProfile.Colors);
    }

    [Fact]
    public void Only_offered_icons_and_colors_are_allowed()
    {
        var p = UserProfile.CreateDefault(Guid.NewGuid(), "a@b.test", TestData.Now);
        p.Update(" Anna ", UserProfile.Icons[1], UserProfile.Colors[2].ToUpperInvariant(), TestData.Now);
        Assert.Equal("Anna", p.DisplayName);
        Assert.Equal(UserProfile.Colors[2], p.AvatarColor);
        Assert.Throws<DomainException>(() => p.Update("Anna", "X", UserProfile.Colors[0], TestData.Now));
        Assert.Throws<DomainException>(() => p.Update("Anna", UserProfile.Icons[0], "#123456", TestData.Now));
        Assert.Throws<DomainException>(() => p.Update("   ", UserProfile.Icons[0], UserProfile.Colors[0], TestData.Now));
    }

    [Fact]
    public void Avatar_format_is_detected_from_the_content()
    {
        Assert.Equal("image/jpeg", AvatarImage.DetectType([0xFF, 0xD8, 0xFF, 0xE0]));
        Assert.Equal("image/png", AvatarImage.DetectType([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0]));
        Assert.Equal("image/webp", AvatarImage.DetectType("RIFF\0\0\0\0WEBPVP8 "u8));
        Assert.Null(AvatarImage.DetectType("<svg onload=alert(1)>"u8));
        Assert.Throws<DomainException>(() => AvatarImage.Create(Guid.NewGuid(), new byte[AvatarImage.MaxBytes + 1], TestData.Now));
    }
}

public class WordSetTests
{
    [Fact]
    public void Set_removes_duplicates_and_keeps_order()
    {
        var f = Guid.NewGuid();
        var set = WordSet.Create(TestData.Owner, TestData.LanguageId, "Freitag",
            [new(f, "b"), new(f, "a"), new(f, "b")], TestData.Now);
        Assert.Equal(["b", "a"], set.Items.Select(i => i.WordKey));
        Assert.Throws<DomainException>(() => set.Update("Freitag", [], TestData.Now));
    }
}

public class GroupTests
{
    private static readonly Guid Leader = Guid.NewGuid();
    private static readonly Guid Member = Guid.NewGuid();

    private static Group NewGroup()
    {
        var g = Group.Create(Leader, "Klasse 3b", Group.NewJoinCode(), TestData.Now);
        g.Join(Member, TestData.Now);
        return g;
    }

    [Fact]
    public void Join_codes_are_readable_and_normalized()
    {
        var code = Group.NewJoinCode();
        Assert.Equal(Group.CodeLength, code.Length);
        Assert.DoesNotContain(code, c => "01OIL".Contains(c));
        Assert.Equal("ABCD2345", Group.NormalizeCode(" abcd-2345 "));
    }

    [Fact]
    public void Only_the_leader_manages_the_group()
    {
        var g = NewGroup();
        Assert.True(g.IsLeader(Leader));
        Assert.False(g.IsLeader(Member));
        Assert.Throws<DomainException>(() => g.Rename(Member, "Neu"));
        Assert.Throws<DomainException>(() => g.Share(Member, Guid.NewGuid(), null, TestData.Now));
        Assert.Throws<DomainException>(() => g.RemoveMember(Member, Leader));
        g.Rename(Leader, "Klasse 4b");
        Assert.Equal("Klasse 4b", g.Name);
    }

    [Fact]
    public void Leader_cannot_leave_but_members_can()
    {
        var g = NewGroup();
        Assert.Throws<DomainException>(() => g.Leave(Leader));
        g.Leave(Member);
        Assert.False(g.IsMember(Member));
        Assert.Throws<DomainException>(() => g.Leave(Member));
    }

    [Fact]
    public void Joining_twice_is_harmless_and_sharing_twice_is_not_allowed()
    {
        var g = NewGroup();
        g.Join(Member, TestData.Now);
        Assert.Equal(2, g.Members.Count);

        var folder = Guid.NewGuid();
        var share = g.Share(Leader, folder, null, TestData.Now);
        Assert.Throws<DomainException>(() => g.Share(Leader, folder, null, TestData.Now));
        Assert.Throws<DomainException>(() => g.Share(Leader, null, null, TestData.Now));
        g.Unshare(Leader, share.Id);
        Assert.Empty(g.Shares);
    }
}
