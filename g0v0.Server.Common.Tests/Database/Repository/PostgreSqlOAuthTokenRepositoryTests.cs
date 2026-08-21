// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class PostgreSqlOAuthTokenRepositoryTests
{
    private PostgreSqlDbContext _context = null!;
    private OAuthTokenRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<PostgreSqlDbContext> options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PostgreSqlDbContext(options);
        _repository = new OAuthTokenRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    #region GetByAccessTokenAsync

    [Test]
    public async Task GetByAccessTokenAsync_WhenTokenExistsAndNotExpired_ShouldReturnToken()
    {
        OAuthToken token = new()
        {
            AccessToken = "pg_valid_token",
            RefreshToken = "pg_refresh_1",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        OAuthToken? result = await _repository.GetByAccessTokenAsync("pg_valid_token");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AccessToken, Is.EqualTo("pg_valid_token"));
    }

    [Test]
    public async Task GetByAccessTokenAsync_WhenTokenExpired_ShouldReturnNull()
    {
        OAuthToken token = new()
        {
            AccessToken = "pg_expired_token",
            RefreshToken = "pg_refresh_2",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        OAuthToken? result = await _repository.GetByAccessTokenAsync("pg_expired_token");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByAccessTokenAsync_WhenTokenDoesNotExist_ShouldReturnNull()
    {
        OAuthToken? result = await _repository.GetByAccessTokenAsync("pg_nonexistent");

        Assert.That(result, Is.Null);
    }

    #endregion

    #region CheckAccessTokenIsValidAsync

    [Test]
    public async Task CheckAccessTokenIsValidAsync_WhenTokenExistsAndNotExpired_ShouldReturnTrue()
    {
        OAuthToken token = new()
        {
            AccessToken = "pg_check_valid",
            RefreshToken = "pg_refresh_3",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        bool result = await _repository.CheckAccessTokenIsValidAsync("pg_check_valid");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CheckAccessTokenIsValidAsync_WhenTokenExpired_ShouldReturnFalse()
    {
        OAuthToken token = new()
        {
            AccessToken = "pg_check_expired",
            RefreshToken = "pg_refresh_4",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        bool result = await _repository.CheckAccessTokenIsValidAsync("pg_check_expired");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CheckAccessTokenIsValidAsync_WhenTokenDoesNotExist_ShouldReturnFalse()
    {
        bool result = await _repository.CheckAccessTokenIsValidAsync("pg_nonexistent");

        Assert.That(result, Is.False);
    }

    #endregion

    #region Marker interface

    [Test]
    public void OAuthTokenRepository_ShouldImplementIPostgreSqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IPostgreSqlRepository>());
    }

    #endregion
}