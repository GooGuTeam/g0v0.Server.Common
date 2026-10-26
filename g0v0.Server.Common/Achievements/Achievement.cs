// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Achievements;

/// <summary>A medal definition shared by clients and the legacy lazer achievement IDs.</summary>
/// <param name="Id">The persistent achievement ID.</param>
/// <param name="Name">The display name.</param>
/// <param name="Description">The unlock description.</param>
/// <param name="Icon">The medal asset name without its extension.</param>
public sealed record Achievement(int Id, string Name, string Description, string Icon);