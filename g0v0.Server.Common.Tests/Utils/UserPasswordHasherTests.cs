// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Utils;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Utils;

public class UserPasswordHasherTests
{
    // The MD5 of "hunter22", the digest the stable client sends for that password.
    private const string Password = "hunter22";
    private const string Digest = "cb95015a436fe976eb38e45455372032";

    [Test]
    public void ComputesTheLowercaseHexDigestTheClientSends()
    {
        Assert.That(UserPasswordHasher.ComputeDigest(Password), Is.EqualTo(Digest));
    }

    [Test]
    public void StoresABcryptHashOfTheDigestRatherThanThePlainText()
    {
        string hash = UserPasswordHasher.HashDigest(Digest, workFactor: 4);

        Assert.Multiple(() =>
        {
            // Verification happens with the digest the login handshake receives.
            Assert.That(UserPasswordHasher.VerifyDigest(Digest, hash), Is.True);
            Assert.That(UserPasswordHasher.VerifyPassword(Password, hash), Is.True);

            // A hash of the plain text would never match a stable login.
            Assert.That(BCrypt.Net.BCrypt.Verify(Password, hash), Is.False);
            Assert.That(UserPasswordHasher.VerifyDigest(UserPasswordHasher.ComputeDigest("hunter23"), hash), Is.False);
        });
    }

    [Test]
    public void RejectsAMalformedStoredHashInsteadOfThrowing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UserPasswordHasher.VerifyDigest(Digest, "$2b$not-a-hash"), Is.False);
            Assert.That(UserPasswordHasher.VerifyDigest(Digest, hash: null), Is.False);
            Assert.That(UserPasswordHasher.VerifyDigest(digest: null, hash: "$2b$04$abcdefghijklmnopqrstuuvRd6479mlTDECYXOIYMV5.3yuvdazPi"), Is.False);
        });
    }

    [Test]
    public void OnlyAcceptsThirtyTwoHexadecimalCharactersAsADigest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UserPasswordHasher.IsValidDigest(Digest), Is.True);
            Assert.That(UserPasswordHasher.IsValidDigest(Digest.ToUpperInvariant()), Is.True);
            Assert.That(UserPasswordHasher.IsValidDigest(null), Is.False);
            Assert.That(UserPasswordHasher.IsValidDigest(Digest[..31]), Is.False);
            Assert.That(UserPasswordHasher.IsValidDigest(new string('g', 32)), Is.False);
        });
    }

    [Test]
    public void VerifyPasswordRejectsAnEmptySubmission()
    {
        string hash = UserPasswordHasher.HashDigest(Digest, workFactor: 4);

        Assert.Multiple(() =>
        {
            Assert.That(UserPasswordHasher.VerifyPassword(password: null, hash), Is.False);
            Assert.That(UserPasswordHasher.VerifyPassword(string.Empty, hash), Is.False);
        });
    }
}