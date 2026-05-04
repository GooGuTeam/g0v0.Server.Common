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

    [Test]
    public async Task HandleRequirementAsync_WithMatchingClientId_ShouldSucceed()
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        var manager = new ConfigurationManager(_tempDir);
        var handler = new ScopeAuthorizationHandler(manager);
        var requirement = new ScopeAuthorizationRequirement("chat.read");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OAuthClaimTypes.ClientId, "100"),
        }));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task HandleRequirementAsync_WithMatchingWebClientId_ShouldSucceed()
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        var manager = new ConfigurationManager(_tempDir);
        var handler = new ScopeAuthorizationHandler(manager);
        var requirement = new ScopeAuthorizationRequirement("chat.read");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OAuthClaimTypes.ClientId, "200"),
        }));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task HandleRequirementAsync_WithNonMatchingClientId_ShouldNotSucceedWithoutScopes()
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        var manager = new ConfigurationManager(_tempDir);
        var handler = new ScopeAuthorizationHandler(manager);
        var requirement = new ScopeAuthorizationRequirement("chat.read");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OAuthClaimTypes.ClientId, "999"),
        }));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleRequirementAsync_WithWildcardScope_ShouldSucceed()
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        var manager = new ConfigurationManager(_tempDir);
        var handler = new ScopeAuthorizationHandler(manager);
        var requirement = new ScopeAuthorizationRequirement("chat.read", "chat.write");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OAuthClaimTypes.Scope, "*"),
        }));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

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

        var manager = new ConfigurationManager(_tempDir);
        var handler = new ScopeAuthorizationHandler(manager);
        var requirement = new ScopeAuthorizationRequirement("chat.read", "chat.write");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OAuthClaimTypes.Scope, "chat.read"),
            new Claim(OAuthClaimTypes.Scope, "chat.write"),
        }));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

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

        var manager = new ConfigurationManager(_tempDir);
        var handler = new ScopeAuthorizationHandler(manager);
        var requirement = new ScopeAuthorizationRequirement("chat.read", "admin");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OAuthClaimTypes.Scope, "chat.read"),
        }));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

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

        var manager = new ConfigurationManager(_tempDir);
        var handler = new ScopeAuthorizationHandler(manager);
        var requirement = new ScopeAuthorizationRequirement("chat.read");
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleRequirementAsync_WithMultipleRequirements_ShouldSucceedForPartialMatch()
    {
        WriteGeneralConfig(new
        {
            OsuClientId = 100,
            OsuWebClientId = 200,
        });

        var manager = new ConfigurationManager(_tempDir);
        var handler = new ScopeAuthorizationHandler(manager);
        var requirement1 = new ScopeAuthorizationRequirement("chat.read");
        var requirement2 = new ScopeAuthorizationRequirement("chat.write");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OAuthClaimTypes.Scope, "chat.read"),
        }));
        var context = new AuthorizationHandlerContext(new[] { requirement1, requirement2 }, user, null);

        await handler.HandleAsync(context);

        // requirement1 should succeed, requirement2 should not
        Assert.That(context.HasSucceeded, Is.False);
    }

    private void WriteGeneralConfig(object content)
    {
        File.WriteAllText(
            Path.Combine(_configDir, "general.json"),
            JsonConvert.SerializeObject(content));
    }
}