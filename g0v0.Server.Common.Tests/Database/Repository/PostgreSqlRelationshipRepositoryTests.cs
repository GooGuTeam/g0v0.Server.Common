// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class PostgreSqlRelationshipRepositoryTests
{
    private PostgreSqlDbContext _context = null!;
    private RelationshipRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PostgreSqlDbContext(options);
        _repository = new RelationshipRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    #region GetByUserIdAsync

    [Test]
    public async Task GetByUserIdAsync_WhenRelationshipsExist_ShouldReturnAllForUser()
    {
        var relationship1 = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Follow,
        };
        var relationship2 = new Relationship
        {
            UserId = 1,
            TargetId = 3,
            Type = RelationshipType.Block,
        };
        var relationship3 = new Relationship
        {
            UserId = 4,
            TargetId = 1,
            Type = RelationshipType.Follow,
        };
        _context.Relationships.AddRange(relationship1, relationship2, relationship3);
        await _context.SaveChangesAsync();

        IReadOnlyList<Relationship> results = await _repository.GetByUserIdAsync(1);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results, Has.All.Matches<Relationship>(r => r.UserId == 1));
    }

    [Test]
    public async Task GetByUserIdAsync_WhenNoRelationships_ShouldReturnEmpty()
    {
        IReadOnlyList<Relationship> results = await _repository.GetByUserIdAsync(999);

        Assert.That(results, Is.Empty);
    }

    #endregion

    #region GetByTargetIdAsync

    [Test]
    public async Task GetByTargetIdAsync_WhenRelationshipsExist_ShouldReturnAllForTarget()
    {
        var relationship1 = new Relationship
        {
            UserId = 1,
            TargetId = 5,
            Type = RelationshipType.Follow,
        };
        var relationship2 = new Relationship
        {
            UserId = 2,
            TargetId = 5,
            Type = RelationshipType.Follow,
        };
        var relationship3 = new Relationship
        {
            UserId = 5,
            TargetId = 1,
            Type = RelationshipType.Block,
        };
        _context.Relationships.AddRange(relationship1, relationship2, relationship3);
        await _context.SaveChangesAsync();

        IReadOnlyList<Relationship> results = await _repository.GetByTargetIdAsync(5);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results, Has.All.Matches<Relationship>(r => r.TargetId == 5));
    }

    [Test]
    public async Task GetByTargetIdAsync_WhenNoRelationships_ShouldReturnEmpty()
    {
        IReadOnlyList<Relationship> results = await _repository.GetByTargetIdAsync(999);

        Assert.That(results, Is.Empty);
    }

    #endregion

    #region GetRelationshipAsync

    [Test]
    public async Task GetRelationshipAsync_WhenRelationshipExists_ShouldReturnIt()
    {
        var relationship = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Follow,
        };
        _context.Relationships.Add(relationship);
        await _context.SaveChangesAsync();

        Relationship? result = await _repository.GetRelationshipAsync(1, 2);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(1));
        Assert.That(result.TargetId, Is.EqualTo(2));
        Assert.That(result.Type, Is.EqualTo(RelationshipType.Follow));
    }

    [Test]
    public async Task GetRelationshipAsync_WhenRelationshipDoesNotExist_ShouldReturnNull()
    {
        Relationship? result = await _repository.GetRelationshipAsync(1, 2);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetRelationshipAsync_WhenOnlyReversedRelationshipExists_ShouldReturnNull()
    {
        var relationship = new Relationship
        {
            UserId = 2,
            TargetId = 1,
            Type = RelationshipType.Follow,
        };
        _context.Relationships.Add(relationship);
        await _context.SaveChangesAsync();

        Relationship? result = await _repository.GetRelationshipAsync(1, 2);

        Assert.That(result, Is.Null);
    }

    #endregion

    #region IsFollowingAsync

    [Test]
    public async Task IsFollowingAsync_WhenFollowRelationshipExists_ShouldReturnTrue()
    {
        var relationship = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Follow,
        };
        _context.Relationships.Add(relationship);
        await _context.SaveChangesAsync();

        bool result = await _repository.IsFollowingAsync(1, 2);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsFollowingAsync_WhenBlockRelationshipExists_ShouldReturnFalse()
    {
        var relationship = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Block,
        };
        _context.Relationships.Add(relationship);
        await _context.SaveChangesAsync();

        bool result = await _repository.IsFollowingAsync(1, 2);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsFollowingAsync_WhenNoRelationshipExists_ShouldReturnFalse()
    {
        bool result = await _repository.IsFollowingAsync(1, 2);

        Assert.That(result, Is.False);
    }

    #endregion

    #region IsBlockedAsync

    [Test]
    public async Task IsBlockedAsync_WhenBlockRelationshipExists_ShouldReturnTrue()
    {
        var relationship = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Block,
        };
        _context.Relationships.Add(relationship);
        await _context.SaveChangesAsync();

        bool result = await _repository.IsBlockedAsync(1, 2);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsBlockedAsync_WhenFollowRelationshipExists_ShouldReturnFalse()
    {
        var relationship = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Follow,
        };
        _context.Relationships.Add(relationship);
        await _context.SaveChangesAsync();

        bool result = await _repository.IsBlockedAsync(1, 2);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsBlockedAsync_WhenNoRelationshipExists_ShouldReturnFalse()
    {
        bool result = await _repository.IsBlockedAsync(1, 2);

        Assert.That(result, Is.False);
    }

    #endregion

    #region CreateAsync

    [Test]
    public async Task CreateAsync_ShouldAddRelationshipToDatabase()
    {
        var relationship = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Follow,
        };

        await _repository.CreateAsync(relationship);

        Relationship? saved = await _context.Relationships
            .Where(r => r.UserId == 1 && r.TargetId == 2)
            .FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Type, Is.EqualTo(RelationshipType.Follow));
    }

    [Test]
    public async Task CreateAsync_ShouldSetId()
    {
        var relationship = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Follow,
        };

        await _repository.CreateAsync(relationship);

        Assert.That(relationship.Id, Is.Not.EqualTo(0));
    }

    #endregion

    #region UpdateAsync

    [Test]
    public async Task UpdateAsync_ShouldUpdateRelationshipInDatabase()
    {
        var relationship = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Follow,
        };
        _context.Relationships.Add(relationship);
        await _context.SaveChangesAsync();

        relationship.Type = RelationshipType.Block;
        await _repository.UpdateAsync(relationship);

        Relationship? updated = await _context.Relationships
            .Where(r => r.UserId == 1 && r.TargetId == 2)
            .FirstOrDefaultAsync();
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Type, Is.EqualTo(RelationshipType.Block));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_ShouldRemoveRelationshipFromDatabase()
    {
        var relationship = new Relationship
        {
            UserId = 1,
            TargetId = 2,
            Type = RelationshipType.Follow,
        };
        _context.Relationships.Add(relationship);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(relationship);

        bool exists = await _context.Relationships
            .AnyAsync(r => r.UserId == 1 && r.TargetId == 2);
        Assert.That(exists, Is.False);
    }

    #endregion

    #region Marker interface

    [Test]
    public void RelationshipRepository_ShouldImplementIPostgreSqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IPostgreSqlRepository>());
    }

    #endregion
}