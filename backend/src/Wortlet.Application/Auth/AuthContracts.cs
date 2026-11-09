using System.ComponentModel.DataAnnotations;
using Wortlet.Application.Common;

namespace Wortlet.Application.Auth;

public sealed record RegisterRequest(
    [Required(ErrorMessage = "Bitte gib deine E-Mail-Adresse ein.")]
    [EmailAddress(ErrorMessage = "Das ist keine gültige E-Mail-Adresse.")]
    [StringLength(254, ErrorMessage = "Die E-Mail-Adresse ist zu lang.")] string Email,
    [Required(ErrorMessage = "Bitte gib ein Passwort ein.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Das Passwort muss mindestens 8 Zeichen lang sein.")] string Password);

public sealed record LoginRequest(
    [Required(ErrorMessage = "Bitte gib deine E-Mail-Adresse ein.")] string Email,
    [Required(ErrorMessage = "Bitte gib dein Passwort ein.")] string Password);

public sealed record UserDto(Guid Id, string Email);

public sealed record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public sealed record CreateUserResult(Guid? UserId, IReadOnlyList<string> Errors);

public enum CredentialCheck { Success, Invalid, LockedOut }

public sealed record CredentialResult(CredentialCheck Result, Guid UserId = default, string Email = "");

/// <summary>Benutzerkonten (umgesetzt mit ASP.NET Core Identity in der Infrastruktur).</summary>
public interface IIdentityService
{
    Task<CreateUserResult> CreateUserAsync(string email, string password, CancellationToken ct = default);
    Task<CredentialResult> CheckCredentialsAsync(string email, string password, CancellationToken ct = default);
    Task<string?> GetEmailAsync(Guid userId, CancellationToken ct = default);
}

public interface ITokenService
{
    AccessToken Create(Guid userId, string email);
}

public sealed class AuthenticationFailedException(string message) : AppException(message);
