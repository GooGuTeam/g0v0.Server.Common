// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Database.Repository;

public abstract class LoginRepositoryTestsBase
{
    private DbContext _context = null!;
    private ILoginRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _context = CreateContext();
        _repository = CreateRepository(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task ReadsTheStoredPasswordHash()
    {
        User user = new() { Id = 1, Username = "alice", PwBcrypt = "$2b$test" };
        _context.Add(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        Assert.That(await _repository.GetPasswordHashAsync(1), Is.EqualTo("$2b$test"));
        Assert.That(await _repository.GetPasswordHashAsync(2), Is.Null);
    }

    [Test]
    public async Task SavesDeviceSessionAndAuditTogetherAndFiltersDeviceLookups()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        TrustedDevice device = new() { UserId = 1, ClientType = "stable", DeviceHash = "hash", UserAgent = "osu!", ExpiresAt = now.AddDays(1) };
        LoginSession session = new() { UserId = 1, Device = device, DeviceHash = "hash", ExpiresAt = now.AddHours(24) };
        LoginLogSession audit = new() { UserId = 1, IpAddress = "127.0.0.1", LoginMethod = "stable_pending_verification" };
        await _repository.RecordAsync(session, device, audit);
        _context.ChangeTracker.Clear();
        Assert.That(await _repository.GetDeviceAsync(1, "stable", "hash"), Is.Not.Null);
        Assert.That(await _repository.GetDeviceAsync(2, "stable", "hash"), Is.Null);
        Assert.That(await _repository.GetDeviceAsync(1, "stable", "other"), Is.Null);
        Assert.That(await _repository.GetSessionAsync(1, "stable", "hash", now), Is.Not.Null);
        Assert.That(await _repository.GetSessionAsync(1, "stable", "hash", now.AddDays(2)), Is.Null);
        Assert.That(await _context.Set<LoginLogSession>().CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task FailedLoginsOnlyCreateAnAuditRecord()
    {
        await _repository.RecordAsync(session: null, device: null, new LoginLogSession
        {
            UserId = 0,
            IpAddress = "127.0.0.1",
            LoginMethod = "stable_password",
            LoginSuccess = false
        });
        Assert.That(await _context.Set<TrustedDevice>().CountAsync(), Is.Zero);
        Assert.That(await _context.Set<LoginSession>().CountAsync(), Is.Zero);
        Assert.That(await _context.Set<LoginLogSession>().CountAsync(), Is.EqualTo(1));
    }

    protected abstract DbContext CreateContext();

    protected abstract ILoginRepository CreateRepository(DbContext context);
}