// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Clothing.Systems;

public sealed partial class ClothingGrantingSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingGrantComponentComponent, GotEquippedEvent>(OnCompEquip);
        SubscribeLocalEvent<ClothingGrantComponentComponent, GotUnequippedEvent>(OnCompUnequip);

        SubscribeLocalEvent<ClothingGrantTagComponent, GotEquippedEvent>(OnTagEquip);
        SubscribeLocalEvent<ClothingGrantTagComponent, GotUnequippedEvent>(OnTagUnequip);
    }

    private void OnCompEquip(EntityUid uid, ClothingGrantComponentComponent component, GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryComp<ClothingComponent>(uid, out var clothing)) return;

        if (!clothing.Slots.HasFlag(args.SlotFlags)) return;

        var target = args.EquipTarget;
        component.Active.Clear();
        foreach (var name in component.Components.Keys)
        {
            var type = Factory.GetRegistration(name).Type;
            if (!HasComp(target, type))
                component.Active.Add(name);
        }
        EntityManager.AddComponents(target, component.Components);
    }

    private void OnCompUnequip(EntityUid uid, ClothingGrantComponentComponent component, GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var target = args.EquipTarget;
        foreach (var name in component.Active)
        {
            var type = Factory.GetRegistration(name).Type;
            RemComp(target, type);
        }
        component.Active.Clear();
    }


    private void OnTagEquip(EntityUid uid, ClothingGrantTagComponent component, GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryComp<ClothingComponent>(uid, out var clothing))
            return;

        if (!clothing.Slots.HasFlag(args.SlotFlags))
            return;

        var target = args.EquipTarget;
        var tags = EnsureComp<TagComponent>(target);
        var tag = component.Tag;
        component.IsActive = !_tag.HasTag(tags, tag);
        if (component.IsActive)
            _tag.AddTag((target, tags), tag);
    }

    private void OnTagUnequip(EntityUid uid, ClothingGrantTagComponent component, GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!component.IsActive)
            return;

        _tag.RemoveTag(args.EquipTarget, component.Tag);
        component.IsActive = false;
    }
}
