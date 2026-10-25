// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;

namespace g0v0.Server.Common.Utils;

/// <summary>
/// Validates the username, email address and password of a new account.
/// </summary>
/// <remarks>
/// Every method returns the human-readable violations, empty when the value is acceptable. The
/// messages are sent to the client verbatim, so keep the wording the clients render.
/// </remarks>
public static class UserCredentialValidator
{
    /// <summary>
    /// The shortest username accepted by the clients.
    /// </summary>
    public const int MinimumUsernameLength = 2;

    /// <summary>
    /// The longest username accepted by the clients.
    /// </summary>
    public const int MaximumUsernameLength = 15;

    /// <summary>
    /// The shortest password this server accepts.
    /// </summary>
    public const int MinimumPasswordLength = 8;

    /// <summary>
    /// The longest password this server accepts.
    /// </summary>
    /// <remarks>
    /// bcrypt only considers the first 72 bytes, so a longer password would silently lose its
    /// trailing characters instead of protecting the account.
    /// </remarks>
    public const int MaximumPasswordLength = 32;

    /// <summary>
    /// The longest email address the legacy user table can store.
    /// </summary>
    public const int MaximumEmailLength = 254;

    private const int MinimumDistinctPasswordCharacters = 4;
    private const int MinimumPasswordCharacterClasses = 2;

    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Matches the usernames the stable client accepts: letters, digits and the separator
    /// characters, 2-15 characters in total.
    /// </summary>
    private static readonly Regex UsernamePattern = new(@"^[\w \[\]-]{2,15}$", RegexOptions.CultureInvariant, PatternTimeout);

    /// <summary>
    /// Matches the email syntax bancho.py accepts: a local part, an @, and at least one dotted
    /// domain label.
    /// </summary>
    private static readonly Regex EmailPattern = new(@"^[^@\s]{1,200}@[^@\s\.]{1,30}(?:\.[^@\.\s]{2,24})+$", RegexOptions.CultureInvariant, PatternTimeout);

    /// <summary>
    /// Validates a username.
    /// </summary>
    /// <param name="username">The submitted username.</param>
    /// <param name="disallowedNames">Names to refuse, compared case-insensitively; optional.</param>
    /// <returns>The violations, empty when the username is acceptable.</returns>
    public static IReadOnlyList<string> ValidateUsername(string? username, IEnumerable<string>? disallowedNames = null)
    {
        List<string> errors = [];
        if (string.IsNullOrEmpty(username))
        {
            errors.Add("Username is required");
            return errors;
        }

        if (username.Length is < MinimumUsernameLength or > MaximumUsernameLength)
        {
            errors.Add($"Must be {MinimumUsernameLength}-{MaximumUsernameLength} characters in length.");
        }

        if (!UsernamePattern.IsMatch(username))
        {
            errors.Add("Username contains non-allowed characters.");
        }

        if (username.Contains(' ', StringComparison.Ordinal) && username.Contains('_', StringComparison.Ordinal))
        {
            errors.Add("May contain \"_\" and \" \", but not both.");
        }

        if (disallowedNames is not null && disallowedNames.Contains(username, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("Disallowed username; pick another.");
        }

        return errors;
    }

    /// <summary>
    /// Validates an email address.
    /// </summary>
    /// <param name="email">The submitted address.</param>
    /// <returns>The violations, empty when the address is acceptable.</returns>
    public static IReadOnlyList<string> ValidateEmail(string? email)
    {
        List<string> errors = [];
        if (string.IsNullOrEmpty(email))
        {
            errors.Add("Email is required");
            return errors;
        }

        if (email.Length > MaximumEmailLength || !EmailPattern.IsMatch(email))
        {
            errors.Add("Invalid email syntax.");
        }

        return errors;
    }

    /// <summary>
    /// Validates the strength of a password.
    /// </summary>
    /// <param name="password">The submitted password.</param>
    /// <returns>The violations, empty when the password is acceptable.</returns>
    /// <remarks>
    /// Besides the length, a password has to be more varied than a repeated character and to mix
    /// at least two of lowercase letters, uppercase letters, digits and symbols. That rejects the
    /// short and single-class passwords credential-stuffing lists are built from without an
    /// operator having to maintain a dictionary of banned words.
    /// </remarks>
    public static IReadOnlyList<string> ValidatePassword(string? password)
    {
        List<string> errors = [];
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required");
            return errors;
        }

        if (password.Length is < MinimumPasswordLength or > MaximumPasswordLength)
        {
            errors.Add($"Must be {MinimumPasswordLength}-{MaximumPasswordLength} characters in length.");
        }

        if (password.Distinct().Count() < MinimumDistinctPasswordCharacters)
        {
            errors.Add($"Must have more than {MinimumDistinctPasswordCharacters - 1} unique characters.");
        }

        if (CountCharacterClasses(password) < MinimumPasswordCharacterClasses)
        {
            errors.Add("Must mix at least two of lowercase letters, uppercase letters, digits and symbols.");
        }

        return errors;
    }

    private static int CountCharacterClasses(string password)
    {
        bool lower = false, upper = false, digit = false, symbol = false;
        foreach (char character in password)
        {
            if (char.IsLower(character))
            {
                lower = true;
            }
            else if (char.IsUpper(character))
            {
                upper = true;
            }
            else if (char.IsAsciiDigit(character))
            {
                digit = true;
            }
            else
            {
                symbol = true;
            }
        }

        return (lower ? 1 : 0) + (upper ? 1 : 0) + (digit ? 1 : 0) + (symbol ? 1 : 0);
    }
}