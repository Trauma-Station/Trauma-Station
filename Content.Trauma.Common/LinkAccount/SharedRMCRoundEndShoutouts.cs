// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Common.LinkAccount;

[Serializable, NetSerializable]
public sealed record SharedRMCRoundEndShoutouts(string? NT)
{
    public const int CharacterLimit = 50;
}
