// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Abductor;
using Content.Medical.Shared.ItemSwitch;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Stealth.Components;

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
        if (ent.Comp.CurrentState == AbductorArmorModeType.Combat)
            return;

        var user = args.Equipee;
        EnsureComp<StealthComponent>(user);
        EnsureComp<StealthOnMoveComponent>(user);
    }

    private void OnUnequipped(Entity<AbductorVestComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.CurrentState == AbductorArmorModeType.Combat)
            return;

        var user = args.Equipee;
        RemComp<StealthComponent>(user);
        RemComp<StealthOnMoveComponent>(user);
    }

    private void OnItemSwitch(EntityUid uid, AbductorVestComponent component, ref ItemSwitchedEvent args)
    {
        if (Enum.TryParse<AbductorArmorModeType>(args.State, ignoreCase: true, out var state))
            component.CurrentState = state;

        var user = Transform(uid).ParentUid;

        if (state == AbductorArmorModeType.Combat)
        {
            if (TryComp<ClothingComponent>(uid, out var clothingComponent))
                _clothing.SetEquippedPrefix(uid, "combat", clothingComponent);

            if (HasComp<MobStateComponent>(user) && HasComp<StealthComponent>(user))
            {
                RemComp<StealthComponent>(user);
                RemComp<StealthOnMoveComponent>(user);
            }
        }
        else
        {
            if (TryComp<ClothingComponent>(uid, out var clothingComponent))
                _clothing.SetEquippedPrefix(uid, null, clothingComponent);

            if (HasComp<MobStateComponent>(user) && !HasComp<StealthComponent>(user))
            {
                AddComp<StealthComponent>(user);
                AddComp<StealthOnMoveComponent>(user);
            }
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
