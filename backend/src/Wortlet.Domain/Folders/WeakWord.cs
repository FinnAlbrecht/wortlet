namespace Wortlet.Domain.Folders;

/// <summary>Ein Eintrag in „Wörter, die ich nicht kann“. Verweist über den deutschen Schlüssel auf ein Wort.</summary>
public sealed class WeakWord
{
    public string WordKey { get; private set; } = string.Empty;
    public DateTime AddedAt { get; private set; }

    private WeakWord() { }

    internal WeakWord(string wordKey, DateTime addedAt)
    {
        WordKey = wordKey;
        AddedAt = addedAt;
    }
}
