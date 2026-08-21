// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PostgreSqlUserRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.UserRepository;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class PostgreSqlUserRepositoryTests
{
    private PostgreSqlDbContext _context = null!;
    private PostgreSqlUserRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PostgreSqlDbContext(options);
        _repository = new PostgreSqlUserRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        _context.Users.Add(CreateUser(id: 20, username: "pg-peppy"));
        await _context.SaveChangesAsync();

        User? result = await _repository.GetByIdAsync(20);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Username, Is.EqualTo("pg-peppy"));
    }

    [Test]
    public async Task GetUsernameByIdAsync_WhenUserExists_ShouldReturnUsername()
    {
        _context.Users.Add(CreateUser(id: 21, username: "pg-username-only"));
        await _context.SaveChangesAsync();

        string? result = await _repository.GetUsernameByIdAsync(21);

        Assert.That(result, Is.EqualTo("pg-username-only"));
    }

    [Test]
    public async Task GetByUsernameAsync_WhenUserExists_ShouldReturnUser()
    {
        _context.Users.Add(CreateUser(id: 22, username: "pg-lookup"));
        await _context.SaveChangesAsync();

        User? result = await _repository.GetByUsernameAsync("pg-lookup");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(22));
    }

    [Test]
    public async Task UsernameExistsAsync_WhenUserMissing_ShouldReturnFalse()
    {
        bool result = await _repository.UsernameExistsAsync("missing");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateAsync_ShouldPersistUser()
    {
        await _repository.CreateAsync(CreateUser(id: 23, username: "pg-created"));

        bool exists = await _context.Users.AnyAsync(user => user.Username == "pg-created");
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var user = CreateUser(id: 24, username: "pg-before");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user.Username = "pg-after";
        await _repository.UpdateAsync(user);

        string? username = await _context.Users
            .Where(u => u.Id == 24)
            .Select(u => u.Username)
            .SingleAsync();
        Assert.That(username, Is.EqualTo("pg-after"));
    }

    [Test]
    public void UserRepository_ShouldImplementIPostgreSqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IPostgreSqlRepository>());
    }

    private static User CreateUser(int id, string username) => new()
    {
        Id = id,
        Username = username,
    };
}