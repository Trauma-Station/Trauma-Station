// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Stunnable;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stam = default!;

    protected virtual void SubscribeBlade()
    {
        SubscribeLocalEvent<SilverMaelstromComponent, GetClothingStunModifierEvent>(OnBladeStunModify);
        SubscribeLocalEvent<SilverMaelstromComponent, DropHandItemsEvent>(OnBladeDropItems,
            before: new[] { typeof(SharedHandsSystem) });

        SubscribeLocalEvent<EventHereticSacraments>(OnSacraments);
    }

    private void OnSacraments(EventHereticSacraments args)
    {
        if (!TryUseAbility(args))
            return;

        StatusNew.TryUpdateStatusEffectDuration(args.Performer, args.Status, args.Time);
    }

    private void OnBladeDropItems(Entity<SilverMaelstromComponent> ent, ref DropHandItemsEvent args)
    {
        args.Handled = true;
    }

    private void OnBladeStunModify(Entity<SilverMaelstromComponent> ent, ref GetClothingStunModifierEvent args)
    {
        args.Modifier *= 0.5f;
    }
}
