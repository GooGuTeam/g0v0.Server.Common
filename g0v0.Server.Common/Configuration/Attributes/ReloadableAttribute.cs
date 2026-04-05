// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Configuration.Attributes;

/// <summary>
/// Indicates that the property can be reloaded at runtime without restarting the application.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReloadableAttribute : Attribute
{
}