// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Goobstation.Shared.Stealth;

public sealed partial class ForcedStealthSystem : EntitySystem
{
    [Dependency] private SharedStealthSystem _stealth = default!;

    [SubscribeLocalEvent]
    private void OnStatusApplied(Entity<ForcedStealthStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (EnsureComp<StealthComponent>(args.Target, out var stealth))
            ent.Comp.OldVisibility = _stealth.GetVisibility(args.Target, stealth);

        stealth.RevealOnAttack = ent.Comp.RevealOnAttack;
        stealth.RevealOnDamage = ent.Comp.RevealOnDamage;
        _stealth.SetVisibility(args.Target, ent.Comp.Visibility, stealth);
    }

    [SubscribeLocalEvent]
    private void OnStatusRemoved(Entity<ForcedStealthStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.OldVisibility is {} visibility)
            _stealth.SetVisibility(args.Target, visibility);
        else
            RemComp<StealthComponent>(args.Target);
    }
}
