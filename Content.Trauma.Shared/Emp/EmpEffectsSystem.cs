// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emp;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Emp;

public sealed partial class EmpEffectsSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnEmpPulse(Entity<EmpEffectsComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = ent.Comp.Disable;
        _effects.ApplyEffects(ent, ent.Comp.Effects, user: args.User);
    }
}
