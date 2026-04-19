// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Authentication;
using g0v0.Server.Common.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Authentication;

[TestFixture]
public class ScopePolicyProviderTests
{
    private static readonly string[] ConfiguredClientIds = ["5", "6"];
    private static readonly string[] RequiredScopes = ["chat.read", "chat.write"];
    private string _basePath = null!;

    [SetUp]
    public void SetUp()
    {
        _basePath = Path.Combine(Path.GetTempPath(), $"{nameof(ScopePolicyProviderTests)}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_basePath, "config"));
        File.WriteAllText(Path.Combine(_basePath, "config", "general.json"), "{}");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, recursive: true);
        }
    }

    [Test]
    public async Task GetPolicyAsync_ClientOnly_ShouldRequireConfiguredClientIds()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync(AuthorizationPolicyNames.ClientOnly);

        Assert.That(policy, Is.Not.Null);
        Assert.That(policy!.Requirements.OfType<DenyAnonymousAuthorizationRequirement>(), Has.One.Items);

        ClaimsAuthorizationRequirement claimRequirement = policy.Requirements
            .OfType<ClaimsAuthorizationRequirement>()
            .Single(r => r.ClaimType == OAuthClaimTypes.ClientId);

        Assert.That(claimRequirement.AllowedValues, Is.EquivalentTo(ConfiguredClientIds));
    }

    [Test]
    public async Task GetPolicyAsync_RequireUserId_ShouldRequireSubjectClaim()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync(AuthorizationPolicyNames.RequireUserId);

        Assert.That(policy, Is.Not.Null);
        Assert.That(policy!.Requirements.OfType<DenyAnonymousAuthorizationRequirement>(), Has.One.Items);

        ClaimsAuthorizationRequirement claimRequirement = policy.Requirements
            .OfType<ClaimsAuthorizationRequirement>()
            .Single();

        Assert.That(claimRequirement.ClaimType, Is.EqualTo(OAuthClaimTypes.Subject));
    }

    [Test]
    public async Task GetPolicyAsync_RequireScope_ShouldBuildScopeRequirement()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync(AuthorizationPolicyNames.RequireScope(RequiredScopes));

        Assert.That(policy, Is.Not.Null);

        ScopeAuthorizationRequirement requirement = policy!.Requirements
            .OfType<ScopeAuthorizationRequirement>()
            .Single();

        Assert.That(requirement.Scopes, Is.EqualTo(RequiredScopes));
    }

    private ScopePolicyProvider CreateProvider()
    {
        var options = Options.Create(new AuthorizationOptions());
        var config = new ConfigurationManager<GeneralConfiguration>(_basePath);

        return new ScopePolicyProvider(options, config);
    }
}