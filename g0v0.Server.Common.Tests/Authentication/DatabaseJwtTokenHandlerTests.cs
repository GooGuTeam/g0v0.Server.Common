// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using System.Text;
using g0v0.Server.Common.Authentication;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Authentication;

[TestFixture]
public class DatabaseJwtTokenHandlerTests
{
    private static readonly SymmetricSecurityKey SigningKey = new("database_token_tests_12345678901234567890"u8.ToArray());

    [Test]
    public async Task ValidateToken_UnregisteredTokenFails()
    {
        TokenValidationResult result = await ValidateAsync("123", databaseUserId: 123, registered: false);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Exception, Is.TypeOf<SecurityTokenValidationException>());
    }

    [Test]
    public async Task ValidateToken_InvalidSignatureFailsBeforeDatabaseLookup()
    {
        TokenValidationResult result = await ValidateAsync("123", databaseUserId: 123, validSignature: false);

        Assert.That(result.IsValid, Is.False);
    }

    private static async Task<TokenValidationResult> ValidateAsync(
        string? subject, int? databaseUserId, bool registered = true, bool validSignature = true)
    {
        List<Claim> claims =
        [
            new(OAuthClaimTypes.UserId, "forged-user"),
            new(OAuthClaimTypes.ClientId, "5"),
            new(OAuthClaimTypes.Scope, "*"),
        ];
        if (subject != null)
        {
            claims.Add(new Claim(OAuthClaimTypes.Subject, subject));
        }

        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
            Expires = DateTime.UtcNow.AddHours(1),
        });
        OAuthToken? record = registered
            ? new OAuthToken
            {
                UserId = databaseUserId,
                ClientId = 42,
                AccessToken = token,
                RefreshToken = "refresh",
                Scope = "chat.read, chat.write",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            }
            : null;
        TestTokenRepository repository = new(record);
        ServiceCollection services = new();
        services.AddSingleton<IOAuthTokenRepository>(repository);
        using ServiceProvider provider = services.BuildServiceProvider();
        DatabaseJwtTokenHandler handler = new(provider, NullLogger<DatabaseJwtTokenHandler>.Instance);
        TokenValidationResult result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = validSignature ? SigningKey : new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wrong_signing_key_12345678901234567890")),
        });
        Assert.That(repository.LookupCount, Is.EqualTo(validSignature ? 1 : 0));
        return result;
    }

    private sealed class TestTokenRepository(OAuthToken? token) : IOAuthTokenRepository
    {
        public int LookupCount { get; private set; }

        public Task<OAuthToken?> GetByAccessTokenAsync(string accessToken)
        {
            LookupCount++;
            return Task.FromResult(string.Equals(token?.AccessToken, accessToken, StringComparison.Ordinal) ? token : null);
        }

        public Task<bool> CheckAccessTokenIsValidAsync(string accessToken) => throw new NotSupportedException();
    }
}