using Wortlet.Application.Auth;
using Wortlet.Application.Common;

namespace Wortlet.Api.Common;

/// <summary>Liest die Benutzer-ID aus dem JWT („sub“).</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var sub = accessor.HttpContext?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id)
                ? id
                : throw new AuthenticationFailedException("Bitte melde dich an.");
        }
    }
}
