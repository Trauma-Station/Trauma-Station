// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Atmos;
using Content.Shared.Inventory;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

public abstract partial class SharedScorchedMantleSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, GetFirestackPassiveModifierEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, ShouldExtinguishInSpaceEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, NoFirestacksUpdateEvent>(_inventory.RelayEvent);

        Subs.SubscribeWithRelay<ScorchedMantleComponent, ShouldExtinguishInSpaceEvent>(OnShouldExtinguish,
            baseEvent: false,
            held: false);
        Subs.SubscribeWithRelay<ScorchedMantleComponent, GetFirestackPassiveModifierEvent>(OnGetFirestackModifier,
            baseEvent: false,
            held: false);
        Subs.SubscribeWithRelay<ScorchedMantleComponent, NoFirestacksUpdateEvent>(OnNoFirestacks,
            baseEvent: false,
            held: false);
        Subs.SubscribeWithRelay<ScorchedMantleComponent, GetFireProtectionEvent>(OnGetProtection,
            baseEvent: true,
            held: false);

        SubscribeLocalEvent<ScorchedMantleComponent, GetItemActionsEvent>(OnGetActions);
    }

    private void OnGetProtection(Entity<ScorchedMantleComponent> ent, ref GetFireProtectionEvent args)
    {
        args.Multiplier = -10f; // Basically ignore fire AP
    }

    private void OnNoFirestacks(Entity<ScorchedMantleComponent> ent, ref NoFirestacksUpdateEvent args)
    {
        if (!TryComp(ent.Comp.Action, out ActionComponent? action) || !action.Toggled)
            return;

        args.Handled = true;
        UpdateFirestacks(args.Uid);
    }

    private void OnGetActions(Entity<ScorchedMantleComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionProto);
    }

    private void OnGetFirestackModifier(Entity<ScorchedMantleComponent> ent, ref GetFirestackPassiveModifierEvent args)
    {
        if (!args.OnFire || args.Resisting)
            return;

        if (!TryComp(ent.Comp.Action, out ActionComponent? action) || !action.Toggled)
        {
            args.Modifier = 0f;
            return;
        }

        args.Modifier *= -ent.Comp.FireStackIncreaseMultiplier;
    }

    private void OnShouldExtinguish(Entity<ScorchedMantleComponent> ent, ref ShouldExtinguishInSpaceEvent args)
    {
        args.Cancelled = true;
    }

    protected virtual void UpdateFirestacks(EntityUid uid) { }
}
