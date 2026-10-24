// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Claims;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace g0v0.Server.Common.Authentication;

/// <summary>
/// Custom JWT token handler that verifies tokens exist in the database
/// (oauth_tokens table) and are not expired. Also populates OAuth claims
/// (osu_user_id, client_id, scope) from the database token record.
/// </summary>
public class DatabaseJwtTokenHandler(
    IServiceProvider serviceProvider,
    ILogger<DatabaseJwtTokenHandler> logger)
    : TokenHandler
{
    private readonly JsonWebTokenHandler _inner = new();

    /// <summary>
    /// Validates a JWT and verifies that the token record exists and is active in the database.
    /// </summary>
    /// <param name="token">The serialized JWT.</param>
    /// <param name="validationParameters">The token validation parameters.</param>
    /// <returns>The token validation result.</returns>
    public override Task<TokenValidationResult> ValidateTokenAsync(
        string token,
        TokenValidationParameters validationParameters)
    {
        return ValidateTokenInternalAsync(token, validationParameters);
    }

    private async Task<TokenValidationResult> ValidateTokenInternalAsync(
        string token,
        TokenValidationParameters validationParameters)
    {
        TokenValidationResult result = await _inner.ValidateTokenAsync(token, validationParameters);

        if (!result.IsValid)
        {
            return result;
        }

        OAuthToken? tokenRecord;

        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IOAuthTokenRepository tokenRepository = scope.ServiceProvider.GetRequiredService<IOAuthTokenRepository>();
            tokenRecord = await tokenRepository.GetByAccessTokenAsync(token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate token against database");
            return new TokenValidationResult
            {
                IsValid = false,
                Exception = new SecurityTokenValidationException("Database token validation failed.", ex),
            };
        }

        if (tokenRecord == null)
        {
            logger.LogWarning("JWT token not found or expired in database");
            return new TokenValidationResult
            {
                IsValid = false,
                Exception = new SecurityTokenValidationException("Token not found or expired in database."),
            };
        }

        ClaimsIdentity identity = result.ClaimsIdentity;

        // These claims are authoritative database values, never values supplied by the JWT.
        foreach (Claim claim in identity.Claims.Where(static claim =>
                     claim.Type is OAuthClaimTypes.UserId or OAuthClaimTypes.ClientId or OAuthClaimTypes.Scope).ToArray())
        {
            identity.RemoveClaim(claim);
        }

        if (tokenRecord.UserId is { } userId)
        {
            identity.AddClaim(new Claim(OAuthClaimTypes.UserId, userId.ToString(CultureInfo.InvariantCulture)));
        }

        identity.AddClaim(new Claim(OAuthClaimTypes.ClientId, tokenRecord.ClientId.ToString(CultureInfo.InvariantCulture)));

        foreach (string scope in tokenRecord.Scope.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            identity.AddClaim(new Claim(OAuthClaimTypes.Scope, scope));
        }

        return result;
    }
}