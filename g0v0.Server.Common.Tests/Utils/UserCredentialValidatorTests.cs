// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Utils;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Utils;

public class UserCredentialValidatorTests
{
    [TestCase("peppy")]
    [TestCase("a b")]
    [TestCase("a_b")]
    [TestCase("user[1]-x")]
    public void AcceptsTheUsernamesTheClientAccepts(string username)
    {
        Assert.That(UserCredentialValidator.ValidateUsername(username), Is.Empty);
    }

    [TestCase("", "Username is required")]
    [TestCase("a", "Must be 2-15 characters in length.")]
    [TestCase("sixteen_chars__x", "Must be 2-15 characters in length.")]
    [TestCase("two words_here", "not both")]
    [TestCase("bad!name", "Username contains non-allowed characters.")]
    public void ReportsTheReasonAUsernameIsRejected(string username, string expected)
    {
        Assert.That(UserCredentialValidator.ValidateUsername(username), Has.Some.Contains(expected));
    }

    [Test]
    public void RefusesAConfiguredDisallowedNameRegardlessOfCasing()
    {
        string[] disallowed = ["peppy"];

        Assert.That(UserCredentialValidator.ValidateUsername("PePPy", disallowed), Has.Some.Contains("Disallowed username"));
        Assert.That(UserCredentialValidator.ValidateUsername("peppy2", disallowed), Is.Empty);
    }

    [TestCase("player@example.com")]
    [TestCase("a.b+c@mail.example.co.uk")]
    public void AcceptsAValidEmail(string email)
    {
        Assert.That(UserCredentialValidator.ValidateEmail(email), Is.Empty);
    }

    [TestCase("", "Email is required")]
    [TestCase("not-an-email", "Invalid email syntax.")]
    [TestCase("player@localhost", "Invalid email syntax.")]
    public void ReportsTheReasonAnEmailIsRejected(string email, string expected)
    {
        Assert.That(UserCredentialValidator.ValidateEmail(email), Has.Some.Contains(expected));
    }

    [TestCase("hunter22")]
    [TestCase("Hunter22!")]
    [TestCase("correct horse 9")]
    public void AcceptsAPasswordThatMeetsTheStrengthPolicy(string password)
    {
        Assert.That(UserCredentialValidator.ValidatePassword(password), Is.Empty);
    }

    [Test]
    public void RejectsAPasswordThatIsTooShortOrTooLong()
    {
        Assert.That(UserCredentialValidator.ValidatePassword("Hun22!"), Has.Some.Contains("Must be 8-32 characters in length."));
        Assert.That(
            UserCredentialValidator.ValidatePassword("Hunter222!" + new string('x', UserCredentialValidator.MaximumPasswordLength)),
            Has.Some.Contains("Must be 8-32 characters in length."));
    }

    [Test]
    public void RejectsAPasswordBuiltFromTooFewDistinctCharacters()
    {
        Assert.That(UserCredentialValidator.ValidatePassword("aaabbb11"), Has.Some.Contains("Must have more than 3 unique characters."));
    }

    [Test]
    public void RejectsASingleCharacterClassPassword()
    {
        Assert.Multiple(() =>
        {
            // Eight characters, but only lowercase letters: the class rule is what refuses it.
            Assert.That(UserCredentialValidator.ValidatePassword("password"), Has.Some.Contains("Must mix at least two of"));
            Assert.That(UserCredentialValidator.ValidatePassword("12345678"), Has.Some.Contains("Must mix at least two of"));
        });
    }

    [Test]
    public void RejectsAnEmptyPasswordWithItsOwnMessage()
    {
        Assert.That(UserCredentialValidator.ValidatePassword(null), Is.EqualTo(new[] { "Password is required" }));
    }
}