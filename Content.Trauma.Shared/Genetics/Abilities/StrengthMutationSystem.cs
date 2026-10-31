// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Weapons;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed partial class StrengthMutationSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGetMeleeDamage(Entity<StrengthMutationComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        args.Damage *= ent.Comp.MeleeModifier;
    }
}
