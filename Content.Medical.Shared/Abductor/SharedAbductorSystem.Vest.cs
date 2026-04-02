// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Medical.Shared.ItemSwitch;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;

namespace Content.Medical.Shared.Abductor;

// TODO SHITMED: this is awful bruh
public abstract partial class SharedAbductorSystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;

    private void InitializeVest()
    {
        SubscribeLocalEvent<AbductorVestComponent, AfterInteractEvent>(OnVestInteract);
        SubscribeLocalEvent<AbductorVestComponent, ItemSwitchedEvent>(OnItemSwitch);
        SubscribeLocalEvent<AbductorVestComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<AbductorVestComponent, GotEquippedEvent>(OnEquipped);
    }

    private void OnEquipped(Entity<AbductorVestComponent> ent, ref GotEquippedEvent args)
    {
        if (TryComp<ClothingComponent>(ent, out var clothingComponent))
            _clothing.SetEquippedPrefix(ent, "stealth", clothingComponent);
    }

    private void OnUnequipped(Entity<AbductorVestComponent> ent, ref GotUnequippedEvent args)
    {
    }

    private void OnItemSwitch(EntityUid uid, AbductorVestComponent component, ref ItemSwitchedEvent args)
    {
        if (Enum.TryParse<AbductorArmorModeType>(args.State, ignoreCase: true, out var state))
            component.CurrentState = state;

        if (state == AbductorArmorModeType.Combat)
        {
            if (TryComp<ClothingComponent>(uid, out var clothingComponent))
                _clothing.SetEquippedPrefix(uid, "combat", clothingComponent);
        }
        else
        {
            if (TryComp<ClothingComponent>(uid, out var clothingComponent))
                _clothing.SetEquippedPrefix(uid, "stealth", clothingComponent);
        }
    }

    private void OnVestInteract(Entity<AbductorVestComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not {} target ||
            !TryComp<AbductorConsoleComponent>(target, out var console))
            return;

        console.Armor = GetNetEntity(ent);
        Dirty(target, console);
        _popup.PopupClient(Loc.GetString("abductors-ui-vest-linked"), ent, args.User);
    }
}
