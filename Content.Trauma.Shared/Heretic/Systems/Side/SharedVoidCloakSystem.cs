// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Religion;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Temperature;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Systems.Side;

public abstract partial class SharedVoidCloakSystem : EntitySystem
{
    [Dependency] private ClothingSystem _clothing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnBeforeHeatExchange(Entity<VoidCloakComponent> ent, ref InventoryRelayedEvent<BeforeHeatExchangeEvent> args)
    {
        if (ent.Comp.Transparent || args.Args.OurTemp > args.Args.OtherTemp)
            return;

        args.Args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnCheckMagicItem(Entity<VoidCloakComponent> ent, ref InventoryRelayedEvent<CheckMagicItemEvent> args)
    {
        if (!ent.Comp.Transparent)
            args.Args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnTerminating(Entity<VoidCloakHoodComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!TryComp(ent, out AttachedClothingComponent? attached))
            return;

        if (TerminatingOrDeleted(attached.AttachedUid))
            return;

        if (!TryComp(attached.AttachedUid, out VoidCloakComponent? comp))
            return;

        MakeCloakVisible(attached.AttachedUid, comp);
    }

    [SubscribeLocalEvent]
    private void OnEntParentChanged(Entity<VoidCloakHoodComponent> ent, ref EntParentChangedMessage args)
    {
        if (!TryComp(ent, out AttachedClothingComponent? attached))
            return;

        if (TerminatingOrDeleted(attached.AttachedUid))
            return;

        if (!TryComp(attached.AttachedUid, out VoidCloakComponent? comp))
            return;

        if (args.Transform.ParentUid == attached.AttachedUid) // If we unequip hood (new parent is cloak)
            MakeCloakVisible(attached.AttachedUid, comp);
        else // If we equip the hood (mew parent is heretic)
            MakeCloakTransparent(attached.AttachedUid, comp);
    }

    private void MakeCloakTransparent(EntityUid cloak, VoidCloakComponent comp)
    {
        comp.Transparent = true;
        _clothing.SetEquippedPrefix(cloak, "transparent-");
        _appearance.SetData(cloak, VoidCloakVisuals.Transparent, true);

        EnsureComp<StripMenuInvisibleComponent>(cloak);
        RemCompDeferred<UnholyItemComponent>(cloak);
        RemCompDeferred<HereticMagicItemComponent>(cloak);
        UpdatePressureProtection(cloak, false);
    }

    private void MakeCloakVisible(EntityUid cloak, VoidCloakComponent comp)
    {
        comp.Transparent = false;
        _clothing.SetEquippedPrefix(cloak, null);
        _appearance.SetData(cloak, VoidCloakVisuals.Transparent, false);

        RemCompDeferred<StripMenuInvisibleComponent>(cloak);
        EnsureComp<UnholyItemComponent>(cloak);
        EnsureComp<HereticMagicItemComponent>(cloak);
        UpdatePressureProtection(cloak, true);
    }

    protected virtual void UpdatePressureProtection(EntityUid cloak, bool enabled)
    {
    }
}
