using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wortlet.Infrastructure.Storage;

namespace Wortlet.Infrastructure.Identity;

/// <summary>Liefert den Signaturschlüssel für JWTs: aus der Konfiguration oder aus einer Datei im Datenordner.</summary>
public sealed class JwtSigningKeyProvider(IOptions<JwtOptions> options, DataDirectory dataDirectory, ILogger<JwtSigningKeyProvider> logger)
{
    private readonly Lazy<SymmetricSecurityKey> _key = new(() => Load(options.Value, dataDirectory, logger));

    public SymmetricSecurityKey Key => _key.Value;

    private static SymmetricSecurityKey Load(JwtOptions options, DataDirectory dataDirectory, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(options.Key))
        {
            var bytes = Encoding.UTF8.GetBytes(options.Key);
            if (bytes.Length < 32)
                throw new InvalidOperationException("Jwt:Key muss mindestens 32 Zeichen lang sein.");
            return new SymmetricSecurityKey(bytes);
        }

        var path = Path.Combine(dataDirectory.Path, "jwt-signing.key");
        if (File.Exists(path))
            return new SymmetricSecurityKey(Convert.FromBase64String(File.ReadAllText(path).Trim()));

        var generated = RandomNumberGenerator.GetBytes(64);
        File.WriteAllText(path, Convert.ToBase64String(generated));
        logger.LogWarning("Kein Jwt:Key konfiguriert. Ein neuer Schlüssel wurde in {Path} gespeichert.", path);
        return new SymmetricSecurityKey(generated);
    }
}
