using System.Text.RegularExpressions;

namespace SafeVault.Helpers;

/// <summary>
/// Sanitizes user input at the application boundary to prevent XSS and injection attacks.
/// Uses a whitelist approach: strips HTML tags, then rejects anything outside allowed characters.
/// </summary>
public static class InputSanitizer
{
    // Matches any HTML/script tag
    private static readonly Regex HtmlTagPattern = new(@"<[^>]*>", RegexOptions.Compiled);

    // Whitelist: letters, digits, underscore, hyphen, dot, space — max 50 chars
    private static readonly Regex UsernameAllowedPattern = new(
        @"^[a-zA-Z0-9_\-\. ]{1,50}$",
        RegexOptions.Compiled
    );

    // Strict email: standard local-part characters only
    private static readonly Regex StrictEmailPattern = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Strips HTML tags and removes any character outside the username whitelist.
    /// Returns an empty string if the result is blank.
    /// </summary>
    public static string SanitizeUsername(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1. Strip HTML/script tags
        var cleaned = HtmlTagPattern.Replace(input.Trim(), string.Empty);

        // 2. Remove every character not in the whitelist (letters, digits, _ - . space)
        cleaned = Regex.Replace(cleaned, @"[^a-zA-Z0-9_\-\. ]", string.Empty);

        // 3. Enforce maximum length
        return cleaned.Length > 50 ? cleaned[..50] : cleaned;
    }

    /// <summary>
    /// Strips HTML tags and validates strict email format.
    /// Returns an empty string if the result is not a valid email.
    /// </summary>
    public static string SanitizeEmail(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1. Strip HTML/script tags
        var cleaned = HtmlTagPattern.Replace(input.Trim(), string.Empty);

        // 2. Enforce RFC 5321 max length
        if (cleaned.Length > 254)
            return string.Empty;

        // 3. Accept only if it matches strict email format (rejects special chars like ' ( ) )
        return StrictEmailPattern.IsMatch(cleaned) ? cleaned : string.Empty;
    }

    /// <summary>Returns true only when the value is non-empty and matches the username whitelist.</summary>
    public static bool IsValidUsername(string? input) =>
        !string.IsNullOrWhiteSpace(input) && UsernameAllowedPattern.IsMatch(input);

    /// <summary>Returns true only when the value is non-empty and matches strict email format.</summary>
    public static bool IsValidEmail(string? input) =>
        !string.IsNullOrWhiteSpace(input) && StrictEmailPattern.IsMatch(input);

    /// <summary>
    /// Strips HTML tags from password input and enforces max storage length.
    /// </summary>
    public static string SanitizePassword(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var cleaned = HtmlTagPattern.Replace(input.Trim(), string.Empty);
        return cleaned.Length > 255 ? cleaned[..255] : cleaned;
    }

    /// <summary>Returns true only when password is non-empty and within storage length.</summary>
    public static bool IsValidPassword(string? input) =>
        !string.IsNullOrWhiteSpace(input) && input.Length <= 255;
}
