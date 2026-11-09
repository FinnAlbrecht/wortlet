using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Wortlet.Application.Auth;
using Wortlet.Application.Common;

namespace Wortlet.Infrastructure.Identity;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, JwtSigningKeyProvider keys, IClock clock) : ITokenService
{
    public AccessToken Create(Guid userId, string email)
    {
        var o = options.Value;
        var now = clock.UtcNow;
        var expires = now.AddDays(o.ExpiryDays);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = o.Issuer,
            Audience = o.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            SigningCredentials = new SigningCredentials(keys.Key, SecurityAlgorithms.HmacSha256),
        };
        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
