using Microsoft.AspNetCore.Identity;

namespace Wortlet.Infrastructure.Identity;

/// <summary>Fehlermeldungen von ASP.NET Core Identity auf Deutsch.</summary>
public sealed class GermanIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "Ein unbekannter Fehler ist aufgetreten." };
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = "Mit dieser E-Mail-Adresse gibt es schon ein Konto. Melde dich an." };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = "Mit dieser E-Mail-Adresse gibt es schon ein Konto. Melde dich an." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = "Das ist keine gültige E-Mail-Adresse." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = "Die E-Mail-Adresse enthält ungültige Zeichen." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Das Passwort muss mindestens {length} Zeichen lang sein." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Das Passwort braucht mindestens eine Ziffer." };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Das Passwort braucht mindestens einen Kleinbuchstaben." };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "Das Passwort braucht mindestens einen Grossbuchstaben." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Das Passwort braucht mindestens ein Sonderzeichen." };
}
