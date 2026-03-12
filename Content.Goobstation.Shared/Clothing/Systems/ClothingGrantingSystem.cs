// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;

namespace Content.Goobstation.Shared.Clothing.Systems;

public sealed class ClothingGrantingSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

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
        if (!TryComp<ClothingComponent>(uid, out var clothing)) return;

        if (!clothing.Slots.HasFlag(args.SlotFlags)) return;

        var user = args.Equipee;
        component.Active.Clear();
        foreach (var name in component.Components.Keys)
        {
            var type = Factory.GetRegistration(name).Type;
            if (!HasComp(user, type))
                component.Active.Add(name);
        }
        EntityManager.AddComponents(user, component.Components);
    }

    private void OnCompUnequip(EntityUid uid, ClothingGrantComponentComponent component, GotUnequippedEvent args)
    {
        var user = args.Equipee;
        foreach (var name in component.Active)
        {
            var type = Factory.GetRegistration(name).Type;
            RemComp(user, type);
        }
        component.Active.Clear();
    }


    private void OnTagEquip(EntityUid uid, ClothingGrantTagComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing))
            return;

        if (!clothing.Slots.HasFlag(args.SlotFlags))
            return;

        var user = args.Equipee;
        var tags = EnsureComp<TagComponent>(user);
        var tag = component.Tag;
        component.IsActive = !_tag.HasTag(tags, tag);
        if (component.IsActive)
            _tag.AddTag((user, tags), tag);
    }

    private void OnTagUnequip(EntityUid uid, ClothingGrantTagComponent component, GotUnequippedEvent args)
    {
        if (!component.IsActive)
            return;

        _tag.RemoveTag(args.Equipee, component.Tag);
        component.IsActive = false;
    }
}
