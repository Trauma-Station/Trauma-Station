// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.Heretic.Components;

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
