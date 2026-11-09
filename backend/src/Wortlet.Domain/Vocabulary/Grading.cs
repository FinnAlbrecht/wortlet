namespace Wortlet.Domain.Vocabulary;

/// <summary>Note nach Schweizer Skala (1–6, halbe Noten), aus dem ersten Durchgang.</summary>
public static class Grading
{
    public static double SwissGrade(int firstRoundCorrect, int total)
    {
        if (total <= 0) return 1;
        var raw = Math.Round(((double)firstRoundCorrect / total * 5 + 1) * 2, MidpointRounding.AwayFromZero) / 2;
        return Math.Max(1, raw);
    }
}
