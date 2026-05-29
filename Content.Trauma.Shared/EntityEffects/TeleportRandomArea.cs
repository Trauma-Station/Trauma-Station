// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Teleports the target entity to a random unblocked area on the same map.
/// Can optionally check for safe atmos etc.
/// </summary>
public sealed partial class TeleportRandomArea : EntityEffectBase<TeleportRandomArea>
{
    /// <summary>
    /// Check for safe atmos gases on target areas.
    /// </summary>
    [DataField]
    public bool Safe;

    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}
