// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Components;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Evenly deals bone damage to each bone.
/// The damage is split between them.
/// </summary>
public sealed partial class AdjustBoneDamage : EntityEffectBase<AdjustBoneDamage>
{
    [DataField(required: true)]
    public FixedPoint2 Amount = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-adjust-bone-damage", ("amount", Amount));
}

public sealed class AdjustBoneDamageEffectSystem : EntityEffectSystem<BodyComponent, AdjustBoneDamage>
{
    [Dependency] private readonly TraumaSystem _trauma = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<AdjustBoneDamage> args)
    {
        var parts = ent.Comp.RootContainer.ContainedEntities;
        if (parts.Count == 0)
            return;

        var root = parts[0];
        var woundables = _wound.GetAllWoundableChildren(root).ToList();
        var amount = args.Effect.Amount / woundables.Count;
        foreach (var woundable in woundables)
        {
            var bones = woundable.Comp.Bone.ContainedEntities;
            if (bones.Count == 0)
                continue;

            var bone = bones[0];
            // Yeah this is less efficient when theres not as many parts damaged but who tf cares,
            // its a bone medication so it should probs be strong enough to ignore this.
            _trauma.ApplyDamageToBone(bones[0], amount);
        }
    }
}
