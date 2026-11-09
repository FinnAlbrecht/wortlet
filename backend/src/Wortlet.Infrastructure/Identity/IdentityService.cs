using Microsoft.AspNetCore.Identity;
using Wortlet.Application.Auth;
using Wortlet.Application.Common;

namespace Wortlet.Infrastructure.Identity;

internal sealed class IdentityService(UserManager<AppUser> users, IClock clock) : IIdentityService
{
    public async Task<CreateUserResult> CreateUserAsync(string email, string password, CancellationToken ct = default)
    {
        var user = new AppUser { Id = Guid.NewGuid(), UserName = email, Email = email, CreatedAt = clock.UtcNow };
        var result = await users.CreateAsync(user, password);
        return result.Succeeded
            ? new CreateUserResult(user.Id, [])
            : new CreateUserResult(null, result.Errors.Select(e => e.Description).Distinct().ToList());
    }

    public async Task<CredentialResult> CheckCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
            return new CredentialResult(CredentialCheck.Invalid);
        if (await users.IsLockedOutAsync(user))
            return new CredentialResult(CredentialCheck.LockedOut);
        if (!await users.CheckPasswordAsync(user, password))
        {
            await users.AccessFailedAsync(user);
            return await users.IsLockedOutAsync(user)
                ? new CredentialResult(CredentialCheck.LockedOut)
                : new CredentialResult(CredentialCheck.Invalid);
        }
        await users.ResetAccessFailedCountAsync(user);
        return new CredentialResult(CredentialCheck.Success, user.Id, user.Email ?? email);
    }

    public async Task<string?> GetEmailAsync(Guid userId, CancellationToken ct = default) =>
        (await users.FindByIdAsync(userId.ToString()))?.Email;
}
