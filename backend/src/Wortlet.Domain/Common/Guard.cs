namespace Wortlet.Domain.Common;

internal static class Guard
{
    public static string Name(string? value, int maxLength, string what)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new DomainException($"{what} darf nicht leer sein.");
        if (trimmed.Length > maxLength)
            throw new DomainException($"{what} darf höchstens {maxLength} Zeichen lang sein.");
        return trimmed;
    }

    public static string Optional(string? value, int maxLength, string what)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{what} darf höchstens {maxLength} Zeichen lang sein.");
        return trimmed;
    }
}
