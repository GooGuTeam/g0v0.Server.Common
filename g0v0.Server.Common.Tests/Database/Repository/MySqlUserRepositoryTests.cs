// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.MySQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using MySqlUserRepository = g0v0.Server.Common.Database.MySQL.Repository.UserRepository;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class MySqlUserRepositoryTests
{
    private MysqlDbContext _context = null!;
    private MySqlUserRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MysqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MysqlDbContext(options);
        _repository = new MySqlUserRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        _context.Users.Add(CreateUser(id: 10, username: "peppy"));
        await _context.SaveChangesAsync();

        User? result = await _repository.GetByIdAsync(10);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Username, Is.EqualTo("peppy"));
    }

    [Test]
    public async Task GetUsernameByIdAsync_WhenUserExists_ShouldReturnUsername()
    {
        _context.Users.Add(CreateUser(id: 11, username: "username-only"));
        await _context.SaveChangesAsync();

        string? result = await _repository.GetUsernameByIdAsync(11);

        Assert.That(result, Is.EqualTo("username-only"));
    }

    [Test]
    public async Task GetByUsernameAsync_WhenUserExists_ShouldReturnUser()
    {
        _context.Users.Add(CreateUser(id: 12, username: "lookup"));
        await _context.SaveChangesAsync();

        User? result = await _repository.GetByUsernameAsync("lookup");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(12));
    }

    [Test]
    public async Task UsernameExistsAsync_WhenUserExists_ShouldReturnTrue()
    {
        _context.Users.Add(CreateUser(id: 13, username: "taken"));
        await _context.SaveChangesAsync();

        bool result = await _repository.UsernameExistsAsync("taken");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CreateAsync_ShouldPersistUser()
    {
        await _repository.CreateAsync(CreateUser(id: 14, username: "created"));

        bool exists = await _context.Users.AnyAsync(user => user.Username == "created");
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var user = CreateUser(id: 15, username: "before");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user.Username = "after";
        await _repository.UpdateAsync(user);

        string? username = await _context.Users
            .Where(u => u.Id == 15)
            .Select(u => u.Username)
            .SingleAsync();
        Assert.That(username, Is.EqualTo("after"));
    }

    [Test]
    public void UserRepository_ShouldImplementIMySqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IMySqlRepository>());
    }

    private static User CreateUser(int id, string username) => new()
    {
        Id = id,
        Username = username,
    };
}