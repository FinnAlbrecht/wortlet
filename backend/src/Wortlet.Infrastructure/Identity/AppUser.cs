using Microsoft.AspNetCore.Identity;

namespace Wortlet.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; }
}
