// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Server.Dragon;

// TODO: make separate component for this shit entirely bruh
public sealed partial class DragonComponent
{
    [DataField]
    public EntityUid? SpawnCarpsActionEntity;

    [DataField]
    public EntProtoId SpawnCarpsAction = "ActionRiseFish";

    [DataField]
    public EntProtoId CarpProtoId = "MobCarpDragon";

    [DataField]
    public int CarpAmount = 3;

    [DataField]
    public EntityUid? RoarActionEntity;

    [DataField]
    public EntProtoId RoarAction = "ActionDragonRoar";

    [DataField]
    public float RoarRange = 3f;

    [DataField]
    public float RoarStunTime = 2f;

    [DataField]
    public float CarpRiftHealingRange = 3f;

    /// <summary>
    /// Amount of healing the dragon receives when standing near a carp rift per second.
    /// </summary>
    [DataField]
    public DamageSpecifier CarpRiftHealing;
}
