// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization requirement that ensures the user has a valid Mastodon access token.
/// </summary>
public sealed class MastodonRequirement : IAuthorizationRequirement
{
}
