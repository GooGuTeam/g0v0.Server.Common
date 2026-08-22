// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;

namespace g0v0.Server.Common.Tests.Fetching;

/// <summary>
/// Configures a single scripted HTTP response for a matching request.
/// </summary>
internal sealed record StubResponse(HttpStatusCode StatusCode, string Content, string ContentType = "application/json");