// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Inventory.Events;
using Content.Trauma.Shared.Clothing.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Clothing;

public sealed partial class ClothingEffectsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnEquip(Entity<ClothingEffectsComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _effects.ApplyEffects(args.EquipTarget, ent.Comp.EquipEffects);
    }

    [SubscribeLocalEvent]
    private void OnUnEquipped(Entity<ClothingEffectsComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.UnEquipEffects is { } unEquipEffects)
            _effects.ApplyEffects(args.EquipTarget, unEquipEffects);
    }
}
