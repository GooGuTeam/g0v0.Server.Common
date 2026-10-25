// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace g0v0.Server.Common.Utils;

/// <summary>
/// Hashes and verifies osu! passwords.
/// </summary>
/// <remarks>
/// <para>
/// osu! never stores a password: the client sends the hex MD5 of it and the server stores a bcrypt
/// hash of that digest. Both halves have to agree on this two-step scheme, so hashing on
/// registration and verification on every login live here instead of in the individual servers.
/// </para>
/// </remarks>
public static class UserPasswordHasher
{
    /// <summary>
    /// The length of the hex MD5 digest the osu! clients send.
    /// </summary>
    public const int DigestLength = 32;

    private const int DefaultWorkFactor = 11;

    /// <summary>
    /// Computes the hex MD5 digest of a plain-text password.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The lower-case hex digest the clients send.</returns>
    public static string ComputeDigest(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
#pragma warning disable CA5351 // The osu! wire protocol identifies the password with MD5 before bcrypt.
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();
#pragma warning restore CA5351
    }

    /// <summary>
    /// Decides whether a submitted value has the shape of a password digest.
    /// </summary>
    /// <param name="digest">The candidate digest.</param>
    /// <returns><see langword="true"/> when the value is 32 hexadecimal characters.</returns>
    public static bool IsValidDigest([NotNullWhen(true)] string? digest)
        => digest is { Length: DigestLength } && digest.All(char.IsAsciiHexDigit);

    /// <summary>
    /// Hashes a plain-text password for storage.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The bcrypt hash of the password digest.</returns>
    public static string HashPassword(string password) => HashDigest(ComputeDigest(password));

    /// <summary>
    /// Hashes a password digest for storage.
    /// </summary>
    /// <param name="digest">The hex MD5 digest, normally from <see cref="ComputeDigest"/>.</param>
    /// <param name="workFactor">The bcrypt cost; tests lower it to keep hashing fast.</param>
    /// <returns>The bcrypt hash of the digest.</returns>
    public static string HashDigest(string digest, int workFactor = DefaultWorkFactor)
    {
        ArgumentException.ThrowIfNullOrEmpty(digest);
        return BCrypt.Net.BCrypt.HashPassword(digest.ToLowerInvariant(), workFactor);
    }

    /// <summary>
    /// Verifies a submitted password against a stored hash.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <param name="hash">The stored bcrypt hash.</param>
    /// <returns><see langword="true"/> when the password matches.</returns>
    public static bool VerifyPassword(string? password, string? hash)
        => !string.IsNullOrEmpty(password) && VerifyDigest(ComputeDigest(password), hash);

    /// <summary>
    /// Verifies a submitted password digest against a stored hash.
    /// </summary>
    /// <param name="digest">The hex MD5 digest the client sent.</param>
    /// <param name="hash">The stored bcrypt hash.</param>
    /// <returns><see langword="true"/> when the digest matches.</returns>
    /// <remarks>
    /// A malformed stored hash is a mismatch rather than an error: a legacy row written by another
    /// server must not turn a login attempt into a server fault.
    /// </remarks>
    public static bool VerifyDigest(string? digest, string? hash)
    {
        if (!IsValidDigest(digest) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(digest.ToLowerInvariant(), hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}