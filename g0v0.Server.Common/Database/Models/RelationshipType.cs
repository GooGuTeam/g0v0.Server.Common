// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Defines the type of relationship between two users.
/// </summary>
public enum RelationshipType
{
    /// <summary>
    /// The user is following the target.
    /// </summary>
    Follow,

    /// <summary>
    /// The user has blocked the target.
    /// </summary>
    Block,
}