// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using MySqlDbContext = g0v0.Server.Common.Database.MySQL.MysqlDbContext;
using MySqlOAuthTokenRepository = g0v0.Server.Common.Database.MySQL.Repository.OAuthTokenRepository;
using PostgreSqlDbContext = g0v0.Server.Common.Database.PostgreSQL.PostgreSqlDbContext;
using PostgreSqlOAuthTokenRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.OAuthTokenRepository;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Runs the <see cref="IOAuthTokenRepository"/> contract against both database backends.
/// </summary>
[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class OAuthTokenRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IOAuthTokenRepository _repository = null!;

    private DbSet<OAuthToken> OAuthTokens => _context.Set<OAuthToken>();

    [SetUp]
    public void SetUp()
    {
        _context = RepositoryTestContexts.Create(backend);
        _repository = backend == RepositoryBackend.MySql
            ? new MySqlOAuthTokenRepository((MySqlDbContext)_context)
            : new PostgreSqlOAuthTokenRepository((PostgreSqlDbContext)_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByAccessTokenAsync_WhenTokenExistsAndNotExpired_ShouldReturnToken()
    {
        OAuthToken token = new()
        {
            AccessToken = "valid_token",
            RefreshToken = "refresh_1",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        };
        OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        OAuthToken? result = await _repository.GetByAccessTokenAsync("valid_token");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AccessToken, Is.EqualTo("valid_token"));
    }

    [Test]
    public async Task GetByAccessTokenAsync_WhenTokenExpired_ShouldReturnNull()
    {
        OAuthToken token = new()
        {
            AccessToken = "expired_token",
            RefreshToken = "refresh_2",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        OAuthToken? result = await _repository.GetByAccessTokenAsync("expired_token");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByAccessTokenAsync_WhenTokenDoesNotExist_ShouldReturnNull()
    {
        OAuthToken? result = await _repository.GetByAccessTokenAsync("nonexistent");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CheckAccessTokenIsValidAsync_WhenTokenExistsAndNotExpired_ShouldReturnTrue()
    {
        OAuthToken token = new()
        {
            AccessToken = "check_valid",
            RefreshToken = "refresh_3",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        };
        OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        bool result = await _repository.CheckAccessTokenIsValidAsync("check_valid");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CheckAccessTokenIsValidAsync_WhenTokenExpired_ShouldReturnFalse()
    {
        OAuthToken token = new()
        {
            AccessToken = "check_expired",
            RefreshToken = "refresh_4",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        bool result = await _repository.CheckAccessTokenIsValidAsync("check_expired");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CheckAccessTokenIsValidAsync_WhenTokenDoesNotExist_ShouldReturnFalse()
    {
        bool result = await _repository.CheckAccessTokenIsValidAsync("nonexistent");

        Assert.That(result, Is.False);
    }
}