// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Guardian;

namespace Content.Trauma.Server.Guardian;

/// <summary>
/// Server-side half of the assassin guardian. The stealth burst logic is entirely shared, this
/// concrete subclass exists so the abstract <see cref="SharedGuardianAssassinSystem"/> is
/// actually instantiated and its subscriptions are registered.
/// </summary>
public sealed partial class GuardianAssassinSystem : SharedGuardianAssassinSystem
{
};
