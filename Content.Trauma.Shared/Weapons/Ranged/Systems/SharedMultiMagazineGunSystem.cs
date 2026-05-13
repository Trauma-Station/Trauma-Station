// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedMultiMagazineGunSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, MapInitEvent>(OnMagazineMapInit);
        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, TakeAmmoEvent>(OnMagazineTakeAmmo);
        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, GetAmmoCountEvent>(OnMagazineAmmoCount);
        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, GetVerbsEvent<AlternativeVerb>>(OnMagazineVerb);
        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, EntRemovedFromContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, UseInHandEvent>(OnMagazineUse);
        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, ExaminedEvent>(OnMagazineExamine);
    }

    private void OnMagazineMapInit(Entity<MultiMagazineAmmoProviderComponent> ent, ref MapInitEvent args)
    {
        MagazineSlotChanged(ent);
    }

    private void OnMagazineExamine(Entity<MultiMagazineAmmoProviderComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var ev = new GetAmmoCountEvent();
        RaiseLocalEvent(ent, ref ev);
        args.PushMarkup(Loc.GetString("gun-magazine-examine",
            ("color", SharedGunSystem.AmmoExamineColor),
            ("count", ev.Count)));
    }

    private void OnMagazineUse(Entity<MultiMagazineAmmoProviderComponent> ent, ref UseInHandEvent args)
    {
        var list = new List<EntityUid>();

        foreach (var magEnt in GetMagazineEntities(ent).Values)
        {
            if (magEnt is not { } uid)
                return;

            RaiseLocalEvent(uid, args);
            list.Add(uid);
        }

        _gun.UpdateAmmoCount(ent);
        UpdateMagazineAppearance(ent, list);
    }

    private void OnMagazineVerb(Entity<MultiMagazineAmmoProviderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var list = new List<EntityUid>();

        foreach (var magEnt in GetMagazineEntities(ent).Values)
        {
            if (magEnt is not { } uid)
                return;

            RaiseLocalEvent(uid, args);
            list.Add(uid);
        }

        UpdateMagazineAppearance(ent, list);
    }

    protected virtual void OnMagazineSlotChange(EntityUid uid, MultiMagazineAmmoProviderComponent component, ContainerModifiedMessage args)
    {
        if (!component.Slots.ContainsKey(args.Container.ID))
            return;

        MagazineSlotChanged((uid, component));
    }

    private void MagazineSlotChanged(Entity<MultiMagazineAmmoProviderComponent> ent)
    {
        _gun.UpdateAmmoCount(ent);
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var list = new List<EntityUid>();
        foreach (var magEnt in GetMagazineEntities(ent).Values)
        {
            if (magEnt is { } uid)
                list.Add(uid);
        }

        _appearance.SetData(ent, AmmoVisuals.MagLoaded, list.Count > 0, appearance);
        UpdateMagazineAppearance(ent, list);
    }

    public Dictionary<string, EntityUid?> GetMagazineEntities(Entity<MultiMagazineAmmoProviderComponent> ent)
    {
        var dict = new Dictionary<string, EntityUid?>(ent.Comp.Slots.Count);
        foreach (var magSlot in ent.Comp.Slots.Keys)
        {
            if (!_container.TryGetContainer(ent, magSlot, out var container) ||
                container is not ContainerSlot slot)
            {
                dict[magSlot] = null;
                continue;
            }

            dict[magSlot] = slot.ContainedEntity;
        }

        return dict;
    }

    private void OnMagazineAmmoCount(Entity<MultiMagazineAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        var list = GetMagazineEntities(ent).ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var kvp = list[i];
            if (kvp.Value is not { } e)
            {
                args.Count = 0;
                continue;
            }

            var multiplier = ent.Comp.Slots[kvp.Key] ?? 1f;

            var ev = new GetAmmoCountEvent()
            {
                FireCostMultiplier = multiplier,
            };

            if (i == 0)
            {
                RaiseLocalEvent(e, ref ev);
                args.Count = ev.Count;
                args.Capacity = ev.Capacity;
                continue;
            }

            RaiseLocalEvent(e, ref ev);
            args.Count = Math.Min(ev.Count, args.Count);
            args.Capacity = Math.Min(ev.Capacity, args.Capacity);
        }
    }

    private void OnMagazineTakeAmmo(Entity<MultiMagazineAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        var ev = new GetAmmoCountEvent();
        RaiseLocalEvent(ent, ref ev);
        if (ev.Count < 1)
            return;

        foreach (var (slot, magEnt) in GetMagazineEntities(ent))
        {
            if (magEnt is not { } uid)
                continue;

            // Should we add the projectile from magazine ammo provider or not
            if (ent.Comp.Slots[slot] is not { } multiplier)
            {
                RaiseLocalEvent(uid, args);
                continue;
            }

            var ammoEv = new TakeAmmoEvent(args.Shots, new(), args.Coordinates, args.User)
            {
                FireCostMultiplier = multiplier,
                SpawnProjectiles = false,
            };
            RaiseLocalEvent(uid, ammoEv);
        }
    }

    private void UpdateMagazineAppearance(Entity<MultiMagazineAmmoProviderComponent, AppearanceComponent?> ent,
        List<EntityUid> mags)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        var count = 0;
        var capacity = 0;

        foreach (var magEnt in mags)
        {
            if (!_appearanceQuery.TryComp(magEnt, out var magAppearance))
                continue;

            _appearance.TryGetData<int>(magEnt, AmmoVisuals.AmmoCount, out var addCount, magAppearance);
            _appearance.TryGetData<int>(magEnt, AmmoVisuals.AmmoMax, out var addCapacity, magAppearance);
            count += addCount;
            capacity += addCapacity;
        }

        UpdateMagazineAppearance(ent!, true, count, capacity);
    }

    private void UpdateMagazineAppearance(Entity<MultiMagazineAmmoProviderComponent, AppearanceComponent> ent,
        bool magLoaded,
        int count,
        int capacity)
    {
        _appearance.SetData(ent, AmmoVisuals.MagLoaded, magLoaded, ent.Comp2);
        _appearance.SetData(ent, AmmoVisuals.HasAmmo, count != 0, ent.Comp2);
        _appearance.SetData(ent, AmmoVisuals.AmmoCount, count, ent.Comp2);
        _appearance.SetData(ent, AmmoVisuals.AmmoMax, capacity, ent.Comp2);
    }
}
