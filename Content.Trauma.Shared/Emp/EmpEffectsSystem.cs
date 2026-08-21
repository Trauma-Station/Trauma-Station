// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emp;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Emp;

public sealed partial class EmpEffectsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnEmpPulse(Entity<EmpEffectsComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = ent.Comp.Disable;
        _effects.TryApplyEffect(ent, ent.Comp.Effects, user: args.User);
    }
}
