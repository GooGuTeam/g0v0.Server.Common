// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using g0v0.Server.Common.Authentication;
using g0v0.Server.Common.Configuration;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Authentication;

[TestFixture]
public class ScopeAuthorizationHandlerTests
{
    private string _tempDir = null!;
    private string _configDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _configDir = Path.Combine(_tempDir, "config");
        Directory.CreateDirectory(_configDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [TestCase("100", true)]
    [TestCase("200", true)]
    [TestCase("999", false)]
    public async Task HandleRequirementAsync_WithMatchingClientId_ShouldSucceed(string claimClientId, bool succeed)
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        ConfigManager manager = new(_tempDir);
        ScopeAuthorizationHandler handler = new(manager);
        ScopeAuthorizationRequirement requirement = new("chat.read");
        ClaimsPrincipal user = new(new ClaimsIdentity(
        [
            new Claim(type: OAuthClaimTypes.ClientId, value: claimClientId),
        ]));
        AuthorizationHandlerContext context = new([requirement], user, resource: null);

        await handler.HandleAsync(context);

        if (succeed)
        {
            Assert.That(context.HasSucceeded, Is.True);
        }
        else
        {
            Assert.That(context.HasSucceeded, Is.False);
        }
    }

    [Test]
    public async Task HandleRequirementAsync_WithWildcardScope_ShouldSucceed()
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        ConfigManager manager = new(_tempDir);
        ScopeAuthorizationHandler handler = new(manager);
        ScopeAuthorizationRequirement requirement = new("chat.read", "chat.write");
        ClaimsPrincipal user = new(new ClaimsIdentity(
        [
            new Claim(type: OAuthClaimTypes.Scope, value: "*"),
        ]));
        AuthorizationHandlerContext context = new([requirement], user, resource: null);

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task HandleRequirementAsync_WithAllRequiredScopes_ShouldSucceed()
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        ConfigManager manager = new(_tempDir);
        ScopeAuthorizationHandler handler = new(manager);
        ScopeAuthorizationRequirement requirement = new("chat.read", "chat.write");
        ClaimsPrincipal user = new(new ClaimsIdentity(
        [
            new Claim(type: OAuthClaimTypes.Scope, value: "chat.read"),
            new Claim(type: OAuthClaimTypes.Scope, value: "chat.write"),
        ]));
        AuthorizationHandlerContext context = new([requirement], user, resource: null);

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task HandleRequirementAsync_WithMissingScopes_ShouldNotSucceed()
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        ConfigManager manager = new(_tempDir);
        ScopeAuthorizationHandler handler = new(manager);
        ScopeAuthorizationRequirement requirement = new("chat.read", "admin");
        ClaimsPrincipal user = new(new ClaimsIdentity(
        [
            new Claim(type: OAuthClaimTypes.Scope, value: "chat.read"),
        ]));
        AuthorizationHandlerContext context = new([requirement], user, resource: null);

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleRequirementAsync_WithNoScopeClaims_ShouldNotSucceed()
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        ConfigManager manager = new(_tempDir);
        ScopeAuthorizationHandler handler = new(manager);
        ScopeAuthorizationRequirement requirement = new("chat.read");
        ClaimsPrincipal user = new(new ClaimsIdentity());
        AuthorizationHandlerContext context = new([requirement], user, resource: null);

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    private void WriteGeneralConfig(object content)
    {
        File.WriteAllText(
            Path.Combine(_configDir, "general.json"),
            JsonConvert.SerializeObject(content));
    }
}