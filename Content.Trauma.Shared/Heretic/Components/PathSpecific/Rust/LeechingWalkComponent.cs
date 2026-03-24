// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

[RegisterComponent]
public sealed partial class LeechingWalkComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public FixedPoint2 BoneHeal = -5;

    [DataField]
    public float StaminaHeal = 5f;

    [DataField]
    public float ChemPurgeRate = 3f;

    [DataField]
    public ProtoId<ReagentPrototype>[] ExcludedReagents =
        ["EldritchEssence", "CrucibleSoul", "DuskAndDawn", "WoundedSoldier", "NewbornEther"];

    [DataField]
    public FixedPoint2 BloodHeal = 5f;

    [DataField]
    public TimeSpan StunReduction = TimeSpan.FromSeconds(0.5f);

    [DataField]
    public float TargetTemperature = 310f;

    [DataField]
    public EntProtoId SleepStatus = "StatusEffectForcedSleeping";

    [DataField]
    public EntProtoId DrowsinessStatus = "StatusEffectDrowsiness";

    [DataField]
    public EntProtoId RainbowStatus = "StatusEffectSeeingRainbow";
}
