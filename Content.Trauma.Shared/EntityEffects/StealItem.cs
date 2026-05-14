// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class StealItem : EntityEffectBase<StealItem>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;
}

public sealed partial class StealItemSystem : EntityEffectSystem<HandsComponent, StealItem>
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedWieldableSystem _wield = default!;

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<StealItem> args)
    {
        if (args.User is not { } user)
            return;

        if (!HasComp<HandsComponent>(user))
            return;

        // prioritize active item, but fall back to the first one
        if (!_hands.TryGetActiveItem(ent.AsNullable(), out var item))
        {
            foreach (var hand in ent.Comp.Hands)
            {
                if (_hands.TryGetHeldItem(ent.AsNullable(), hand.Key, out item))
                    break;
            }
        }

        if (item is not { } stolen)
            return;

        if (TryComp<WieldableComponent>(ent, out var wield))
            _wield.TryUnwield(stolen, wield, ent, true);

        if (!_hands.TryDrop(ent.AsNullable(), stolen))
            return;

        _hands.TryPickupAnyHand(user, stolen);
    }
}
