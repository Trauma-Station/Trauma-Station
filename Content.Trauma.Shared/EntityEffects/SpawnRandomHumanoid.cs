// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns random humanoid based on Setings
/// </summary>
public sealed partial class SpawnRandomHumanoid : EntityEffectBase<SpawnRandomHumanoid>
{
    [DataField(required: true)]
    public ProtoId<RandomHumanoidSettingsPrototype> Settings;
}
