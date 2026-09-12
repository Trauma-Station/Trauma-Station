// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Localizations;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Trauma.Shared.Weapons.Ranged;

public sealed partial class AmmoStackReloadSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private EntityQuery<BasicEntityAmmoProviderComponent> _ammoQuery = default!;
    [Dependency] private EntityQuery<StackComponent> _stackQuery = default!;

    private List<string> _names = new();

    [SubscribeLocalEvent]
    private void OnExamined(Entity<AmmoStackReloadComponent> ent, ref ExaminedEvent args)
    {
        _names.Clear();
        foreach (var stack in ent.Comp.Whitelist)
        {
            _names.Add(Loc.GetString(ProtoMan.Index(stack).Name));
        }

        var formatted = ContentLocalizationManager.FormatListToOr(_names);
        args.PushMarkup($"It can be reloaded using a {formatted}.");
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<AmmoStackReloadComponent> ent, ref InteractUsingEvent args)
    {
        var item = args.Used;
        var user = args.User;
        if (!_stackQuery.TryComp(item, out var stack) ||
            !_ammoQuery.TryComp(ent, out var ammo) ||
            // ignore infinite ammo guns
            ammo.Capacity is not { } capacity ||
            ammo.Count is not { } current ||
            !ent.Comp.Whitelist.Contains(stack.StackTypeId))
            return;

        args.Handled = true;

        var limit = capacity - current;
        var count = Math.Min(stack.Count, limit);
        var name = Loc.GetString(ProtoMan.Index(stack.StackTypeId).Name);
        if (count <= 0)
        {
            _popup.PopupEntity($"It can't hold another {name}!", ent, user);
            return;
        }

        _stack.ReduceCount((item, stack), count);

        var plural = count == 1 ? "" : "s";
        _popup.PopupEntity($"You reload {count} {name}{plural}.", ent, user);
        _gun.UpdateBasicEntityAmmoCount((ent, ammo), current + count);
        Dirty(ent, ammo);
    }
}
