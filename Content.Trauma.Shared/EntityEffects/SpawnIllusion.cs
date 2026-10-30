// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Illusion;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns illusion of target mob
/// Requires <see cref="EntityEffectToolComponent"/> to work and the tool must have <see cref="IllusionSpawnerComponent"/>.
/// </summary>
public sealed partial class SpawnIllusion : EntityEffectBase<SpawnIllusion>
{
    /// <summary>
    /// If true tries to aggro illusions on target, potentially harming them
    /// Otherwise aggros on user and marks target as friendly
    /// </summary>
    [DataField]
    public bool TargetIsUser;
}
