// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using MySqlDbContext = g0v0.Server.Common.Database.MySQL.MysqlDbContext;
using MySqlUserRepository = g0v0.Server.Common.Database.MySQL.Repository.UserRepository;
using PostgreSqlDbContext = g0v0.Server.Common.Database.PostgreSQL.PostgreSqlDbContext;
using PostgreSqlUserRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.UserRepository;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Runs the <see cref="IUserRepository"/> contract against both database backends.
/// </summary>
[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class UserRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IUserRepository _repository = null!;

    private DbSet<User> Users => _context.Set<User>();

    [SetUp]
    public void SetUp()
    {
        _context = RepositoryTestContexts.Create(backend);
        _repository = backend == RepositoryBackend.MySql
            ? new MySqlUserRepository((MySqlDbContext)_context)
            : new PostgreSqlUserRepository((PostgreSqlDbContext)_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        Users.Add(CreateUser(id: 10, username: "peppy"));
        await _context.SaveChangesAsync();

        User? result = await _repository.GetByIdAsync(10);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Username, Is.EqualTo("peppy"));
    }

    [Test]
    public async Task GetUsernameByIdAsync_WhenUserExists_ShouldReturnUsername()
    {
        Users.Add(CreateUser(id: 11, username: "username-only"));
        await _context.SaveChangesAsync();

        string? result = await _repository.GetUsernameByIdAsync(11);

        Assert.That(result, Is.EqualTo("username-only"));
    }

    [Test]
    public async Task GetByUsernameAsync_WhenUserExists_ShouldReturnUser()
    {
        Users.Add(CreateUser(id: 12, username: "lookup"));
        await _context.SaveChangesAsync();

        User? result = await _repository.GetByUsernameAsync("lookup");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(12));
    }

    [Test]
    public async Task GetByEmailAsync_WhenUserExists_ShouldReturnThatUser()
    {
        Users.Add(CreateUser(id: 16, username: "mail-a"));
        Users.Add(CreateUser(id: 17, username: "mail-b", email: "b@example.com"));
        await _context.SaveChangesAsync();

        User? result = await _repository.GetByEmailAsync("b@example.com");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(17));
    }

    [Test]
    public async Task GetByEmailAsync_WhenEmailIsUnknown_ShouldReturnNull()
    {
        Users.Add(CreateUser(id: 18, username: "no-mail"));
        await _context.SaveChangesAsync();

        User? result = await _repository.GetByEmailAsync("missing@example.com");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UsernameExistsAsync_WhenUserExists_ShouldReturnTrue()
    {
        Users.Add(CreateUser(id: 13, username: "taken"));
        await _context.SaveChangesAsync();

        bool result = await _repository.UsernameExistsAsync("taken");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UsernameExistsAsync_WhenUserMissing_ShouldReturnFalse()
    {
        bool result = await _repository.UsernameExistsAsync("available");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateAsync_ShouldPersistUser()
    {
        await _repository.CreateAsync(CreateUser(id: 14, username: "created"));

        bool exists = await Users.AnyAsync(user => user.Username == "created");
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task CreateAsync_WithStatisticsModes_ShouldSeedEveryModeForTheNewAccount()
    {
        User user = CreateUser(id: 0, username: "registered");

        await _repository.CreateAsync(user, [0, 1, 2, 3]);

        // The account receives an identity, and the statistics rows are keyed by it.
        Assert.That(user.Id, Is.Not.Zero);
        IQueryable<int> seeded = _context.Set<UserStatistics>()
            .Where(row => row.UserId == user.Id)
            .Select(row => row.Mode);
        Assert.That(seeded, Is.EquivalentTo(new List<int> { 0, 1, 2, 3 }));
    }

    [Test]
    public async Task Playmode_ShouldRoundTripThroughTheBackendEncoding()
    {
        // The legacy table stores these as enum labels, so a broken conversion shows up here.
        User user = CreateUser(id: 0, username: "mode-preference");
        user.Playmode = 2;
        user.G0V0Playmode = 13;

        await _repository.CreateAsync(user);

        _context.ChangeTracker.Clear();
        User reloaded = await Users.SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.Playmode, Is.EqualTo(2));
            Assert.That(reloaded.G0V0Playmode, Is.EqualTo(13));
        });
    }

    [Test]
    public async Task CreateAsync_OnTheLegacySchema_ShouldFillTheColumnsTheModelDoesNotExpose()
    {
        if (backend != RepositoryBackend.MySql)
        {
            // The PostgreSQL schema has no legacy-only columns to fill.
            return;
        }

        User user = CreateUser(id: 0, username: "legacy-columns");

        await _repository.CreateAsync(user);

        Assert.Multiple(() =>
        {
            Assert.That(_context.Entry(user).Property("Priv").CurrentValue, Is.EqualTo(1));
            Assert.That(_context.Entry(user).Property("MaxFriends").CurrentValue, Is.EqualTo(500));
            Assert.That(_context.Entry(user).Property("IsAdmin").CurrentValue, Is.False);
        });
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        User user = CreateUser(id: 15, username: "before");
        Users.Add(user);
        await _context.SaveChangesAsync();

        user.Username = "after";
        await _repository.UpdateAsync(user);

        string? username = await Users
            .Where(u => u.Id == 15)
            .Select(u => u.Username)
            .SingleAsync();
        Assert.That(username, Is.EqualTo("after"));
    }

    private static User CreateUser(int id, string username, string? email = null) => new()
    {
        Id = id,
        Username = username,
        Email = email,
        PwBcrypt = "$2b$test",
    };
}