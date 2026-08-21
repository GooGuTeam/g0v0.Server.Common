// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Authentication;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Authentication;

[TestFixture]
public class AuthorizationAttributeTests
{
    [Test]
    public void ClientOnlyAttribute_ShouldUseClientOnlyPolicy()
    {
        ClientOnlyAttribute attribute = new();

        Assert.That(attribute.Policy, Is.EqualTo(AuthorizationPolicyNames.ClientOnly));
    }

    [Test]
    public void RequireUserIdAttribute_ShouldUseRequireUserIdPolicy()
    {
        RequireUserIdAttribute attribute = new();

        Assert.That(attribute.Policy, Is.EqualTo(AuthorizationPolicyNames.RequireUserId));
    }

    [Test]
    public void RequireScopeAttribute_ShouldEncodeScopesIntoPolicyName()
    {
        RequireScopeAttribute attribute = new("chat.read", "chat.write");

        Assert.That(attribute.Policy, Is.EqualTo("Scope:chat.read,chat.write"));
    }
}