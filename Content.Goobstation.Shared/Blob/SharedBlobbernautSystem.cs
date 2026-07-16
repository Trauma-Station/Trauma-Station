// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;

namespace Content.Goobstation.Shared.Blob;

public abstract partial class SharedBlobbernautSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<BlobbernautComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count < 1)
            return;

        var chem = ProtoMan.Index(ent.Comp.CurrentChem);
        var target = args.HitEntities.FirstOrDefault();
        if (chem.AttackEffects is { } effects)
            _effects.ApplyEffects(target, effects, scale: ent.Comp.AttackEffectsScale, user: args.User);
    }
}
