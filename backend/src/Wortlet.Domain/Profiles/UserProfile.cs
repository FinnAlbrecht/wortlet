using Wortlet.Domain.Common;

namespace Wortlet.Domain.Profiles;

/// <summary>Öffentliches Profil eines Benutzers: Name und Avatar. Die ID ist die Benutzer-ID.</summary>
public sealed class UserProfile : AggregateRoot<Guid>
{
    public const int MaxNameLength = 40;

    /// <summary>Symbole, aus denen man den Avatar wählen kann.</summary>
    public static readonly IReadOnlyList<string> Icons =
        ["🦊", "🐼", "🐸", "🦉", "🐙", "🦁", "🐧", "🐢", "🦄", "🐝", "🐳", "🦖", "🚀", "⭐", "⚽", "🎸", "📚", "🌈"];

    /// <summary>Hintergrundfarben für den Avatar (gut lesbar mit weissem Symbol/Initialen).</summary>
    public static readonly IReadOnlyList<string> Colors =
        ["#2a3c9c", "#1f7a4d", "#b3261e", "#8a5a00", "#6b3fa0", "#0e7490", "#be185d", "#4b5563"];

    public string DisplayName { get; private set; } = string.Empty;
    public string AvatarIcon { get; private set; } = string.Empty;
    public string AvatarColor { get; private set; } = string.Empty;

    /// <summary>Ob ein eigenes Bild hochgeladen ist (es liegt separat in <see cref="AvatarImage"/>).</summary>
    public bool HasImage { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private UserProfile() { }

    /// <summary>Startprofil: Name aus der E-Mail-Adresse, Symbol und Farbe aus der Benutzer-ID abgeleitet.</summary>
    public static UserProfile CreateDefault(Guid userId, string email, DateTime now)
    {
        var local = (email ?? string.Empty).Split('@')[0].Trim();
        if (local.Length == 0) local = "Ich";
        if (local.Length > MaxNameLength) local = local[..MaxNameLength];
        var seed = (int)((uint)userId.GetHashCode() % 10_007);
        return new UserProfile
        {
            Id = userId,
            DisplayName = local,
            AvatarIcon = Icons[seed % Icons.Count],
            AvatarColor = Colors[seed % Colors.Count],
            UpdatedAt = now,
        };
    }

    public void Update(string displayName, string icon, string color, DateTime now)
    {
        DisplayName = Guard.Name(displayName, MaxNameLength, "Der Name");
        if (!Icons.Contains(icon))
            throw new DomainException("Bitte wähle eines der angebotenen Symbole.");
        if (!Colors.Contains(color, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("Bitte wähle eine der angebotenen Farben.");
        AvatarIcon = icon;
        AvatarColor = Colors.First(c => string.Equals(c, color, StringComparison.OrdinalIgnoreCase));
        UpdatedAt = now;
    }

    public void SetHasImage(bool hasImage, DateTime now)
    {
        HasImage = hasImage;
        UpdatedAt = now;
    }
}

/// <summary>Hochgeladenes Avatar-Bild (klein, vom Browser auf 256 px verkleinert).</summary>
public sealed class AvatarImage : AggregateRoot<Guid>
{
    public const int MaxBytes = 300 * 1024;

    public string ContentType { get; private set; } = string.Empty;
    public byte[] Data { get; private set; } = [];
    public DateTime UpdatedAt { get; private set; }

    private AvatarImage() { }

    public static AvatarImage Create(Guid userId, byte[] data, DateTime now)
    {
        var image = new AvatarImage { Id = userId };
        image.Replace(data, now);
        return image;
    }

    /// <summary>Das Format wird am Dateiinhalt erkannt, nicht an der Angabe des Browsers.</summary>
    public void Replace(byte[] data, DateTime now)
    {
        if (data.Length == 0)
            throw new DomainException("Das Bild ist leer.");
        if (data.Length > MaxBytes)
            throw new DomainException($"Das Bild ist zu gross (höchstens {MaxBytes / 1024} KB).");
        var detected = DetectType(data)
            ?? throw new DomainException("Bitte lade ein Bild im Format JPEG, PNG oder WebP hoch.");
        ContentType = detected;
        Data = data;
        UpdatedAt = now;
    }

    /// <summary>Erkennt das Format an den ersten Bytes der Datei.</summary>
    public static string? DetectType(ReadOnlySpan<byte> d)
    {
        if (d.Length >= 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF) return "image/jpeg";
        if (d.Length >= 8 && d[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "image/png";
        if (d.Length >= 12 && d[..4].SequenceEqual("RIFF"u8) && d[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }
}

public interface IProfileRepository
{
    Task<UserProfile?> GetAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserProfile>> ListAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
    void Add(UserProfile profile);

    Task<AvatarImage?> GetImageAsync(Guid userId, CancellationToken ct = default);
    void AddImage(AvatarImage image);
    void RemoveImage(AvatarImage image);
}
