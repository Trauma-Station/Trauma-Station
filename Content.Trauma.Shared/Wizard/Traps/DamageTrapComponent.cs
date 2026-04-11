// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage;

namespace Content.Trauma.Shared.Wizard.Traps;

[RegisterComponent]
public sealed partial class DamageTrapComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public EntProtoId? SpawnedEntity;

    [DataField]
    public TargetBodyPart TargetPart = TargetBodyPart.Feet | TargetBodyPart.Legs;

    [DataField]
    public SplitDamageBehavior SplitDamageBehavior = SplitDamageBehavior.Split;
}
