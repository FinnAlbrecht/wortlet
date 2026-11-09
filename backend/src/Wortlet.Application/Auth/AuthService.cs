using Wortlet.Application.Common;
using Wortlet.Application.Onboarding;
using Wortlet.Application.Profiles;

namespace Wortlet.Application.Auth;

public sealed class AuthService(
    IIdentityService identity,
    ITokenService tokens,
    OnboardingService onboarding,
    ProfileService profiles,
    ICurrentUser currentUser)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        var result = await identity.CreateUserAsync(email, request.Password, ct);
        if (result.UserId is not Guid userId)
            throw new ValidationException(string.Join(" ", result.Errors.Distinct()),
                new Dictionary<string, string[]> { ["email"] = result.Errors.Distinct().ToArray() });

        // Jeder neue Benutzer bekommt ein Profil (Name aus der E-Mail-Adresse) und den Ordner „Einkaufen & Ernährung“.
        await profiles.EnsureAsync(userId, ct);
        await onboarding.SeedNewUserAsync(userId, ct);
        return Issue(userId, email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var check = await identity.CheckCredentialsAsync(request.Email.Trim(), request.Password, ct);
        return check.Result switch
        {
            CredentialCheck.Success => Issue(check.UserId, check.Email),
            CredentialCheck.LockedOut => throw new AuthenticationFailedException("Zu viele Fehlversuche. Warte ein paar Minuten und versuche es dann nochmals."),
            _ => throw new AuthenticationFailedException("E-Mail-Adresse oder Passwort ist falsch."),
        };
    }

    public async Task<UserDto> MeAsync(CancellationToken ct = default)
    {
        var id = currentUser.UserId;
        var email = await identity.GetEmailAsync(id, ct)
            ?? throw new AuthenticationFailedException("Dein Konto wurde nicht gefunden. Bitte melde dich neu an.");
        return new UserDto(id, email);
    }

    private AuthResponse Issue(Guid userId, string email)
    {
        var token = tokens.Create(userId, email);
        return new AuthResponse(token.Token, token.ExpiresAt, new UserDto(userId, email));
    }
}
