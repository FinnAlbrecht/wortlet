namespace Wortlet.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Geheimer Schlüssel (mind. 32 Zeichen). Nie in appsettings.json – per Umgebungsvariable Jwt__Key
    /// oder User Secrets. Fehlt er, wird einer erzeugt und im Datenordner gespeichert.
    /// </summary>
    public string? Key { get; set; }

    public string Issuer { get; set; } = "Wortlet";
    public string Audience { get; set; } = "Wortlet";
    public int ExpiryDays { get; set; } = 30;
}
