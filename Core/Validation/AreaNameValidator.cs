using System.Text.RegularExpressions;

namespace DroneMesh3D.Core.Validation;

public static partial class AreaNameValidator
{
    private const int MaxLength = 50;

    [GeneratedRegex(@"^[\p{L}\p{N}\s\-_\.()]+$")]
    private static partial Regex AllowedCharsRegex();

    /// <summary>
    /// Normalizes and validates an area name. Returns null if input is empty/whitespace.
    /// Throws ArgumentException if name contains disallowed characters.
    /// </summary>
    public static string? NormalizeAndValidate(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var trimmed = name.Trim();
        if (trimmed.Length > MaxLength) trimmed = trimmed[..MaxLength];
        if (!AllowedCharsRegex().IsMatch(trimmed))
            throw new ArgumentException("Name contains disallowed characters.");

        return trimmed;
    }
}
