// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.EntitySystems;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Magic;
using Content.Shared.PDA;
using Content.Shared.Access.Components;
using Content.Shared.Stunnable;
using Content.Shared.Temperature.Systems;
using Content.Trauma.Shared.Magic.Demonologist.Components;
using Content.Trauma.Shared.Magic.Demonologist.Events;

namespace Content.Trauma.Server.Magic.Demonologist;

public sealed partial class DemonologistSystem : EntitySystem
{
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedMagicSystem _magic = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;
    [Dependency] private RoleSystem _roles = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<DemonologistComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ref ent.Comp.CombustionAction, ent.Comp.CombustionActionPrototype);
        _actions.AddAction(ent, ref ent.Comp.BindApprenticeAction, ent.Comp.BindApprenticeActionPrototype);
        _actions.AddAction(ent, ref ent.Comp.BloodBoilAction, ent.Comp.BloodBoilActionPrototype);
    }

    [SubscribeLocalEvent]
    private void OnBloodBoil(BloodBoilSpellEvent args)
    {
        if (args.Handled || !_magic.PassesSpellPrerequisites(args.Action, args.Performer))
            return;

        _temperature.ChangeHeat(args.Target, 350000f, true);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCombustion(CombustionSpellEvent args)
    {
        if (!TryComp<FlammableComponent>(args.Target, out var flammable))
            return;

        _flammable.AdjustFireStacks(args.Target, flammable!.MaximumFireStacks, flammable, ignite: true);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnBindApprentice(BindApprenticeEvent ev) // TODO: also give apprentice demonmind
    {
        if (ev.Handled || !_magic.PassesSpellPrerequisites(ev.Action, ev.Performer))
            return;

        if (HasComp<DemonologistApprenticeComponent>(ev.Target) || HasComp<DemonologistComponent>(ev.Target))
            return;

        if (!_mind.TryGetMind(ev.Target, out var mindId, out _))
            return;

        _stun.TryUpdateParalyzeDuration(ev.Target, ev.ParalyzeDuration);

        EnsureComp<DemonologistApprenticeComponent>(ev.Target);
        _roles.MindAddRole(mindId, "DemonologistApprenticeMindRole");

        SetGear(ev.Target, ev.Gear);

        ev.Handled = true;
    }

    private void SetGear(EntityUid uid, Dictionary<string, EntProtoId> gear, bool force = true)
    {
        if (!TryComp(uid, out InventoryComponent? inventoryComponent))
            return;

        foreach (var (slot, item) in gear)
        {
            _inventory.TryUnequip(uid, slot, true, force, false, inventoryComponent);

            var ent = Spawn(item, Transform(uid).Coordinates);
            if (!_inventory.TryEquip(uid, ent, slot, true, force, false, inventoryComponent))
            {
                Del(ent);
                continue;
            }

            if (slot == "id" &&
                TryComp(ent, out PdaComponent? pdaComponent) &&
                TryComp<IdCardComponent>(pdaComponent.ContainedId, out var id))
                id.FullName = MetaData(uid).EntityName;

            if (HasComp<ClothingComponent>(ent))
                EnsureComp<UnremoveableComponent>(ent);
        }
    }
}
