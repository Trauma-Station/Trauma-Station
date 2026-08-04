// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.StatusEffectNew;

namespace Content.Trauma.Shared.Body.Organ;

public sealed partial class OrganStatusEffectsSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    [SubscribeLocalEvent]
    private void OnEnabled(Entity<OrganStatusEffectsComponent> ent, ref OrganEnabledEvent args)
    {
        _status.AddEffects(args.Body, ent.Comp.StatusEffects);
    }

    [SubscribeLocalEvent]
    private void OnDisabled(Entity<OrganStatusEffectsComponent> ent, ref OrganDisabledEvent args)
    {
        _status.RemoveEffects(args.Body, ent.Comp.StatusEffects);
    }
}
