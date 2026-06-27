// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Materials;

namespace Content.Trauma.Shared.Materials;

public sealed partial class MasterSiloSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedMaterialStorageSystem _material = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private EntityQuery<PhysicalCompositionComponent> _compositionQuery = default!;
    [Dependency] private EntityQuery<StackComponent> _stackQuery = default!;

    private List<Entity<MaterialStorageComponent>> _silos = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MasterSiloComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<MasterSiloFeederComponent, MaterialStorageInsertAttemptEvent>(OnFeedAttempt);
    }

    private void OnInteractUsing(Entity<MasterSiloComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !SiloAccepts(ent.Comp, args.Used))
            return;

        args.Handled = TryDistribute(ent, args.Used, args.User);
    }

    private void OnFeedAttempt(Entity<MasterSiloFeederComponent> ent, ref MaterialStorageInsertAttemptEvent args)
    {
        var item = args.Item;
        if (args.Handled ||
            Transform(ent).GridUid is not { } grid)
            return;

        args.Handled = FindValidMasterSilo(grid, item) is { } silo &&
            SiloAccepts(silo.Comp, item) &&
            TryDistribute(silo, item, args.User);
    }

    public bool SiloAccepts(MasterSiloComponent comp, EntityUid item)
        => _whitelist.CheckBoth(item, comp.Blacklist, comp.Whitelist);

    /// <summary>
    /// Try to distribute a material item to master silo clients, turning true if it was consumed and distributed.
    /// </summary>
    public bool TryDistribute(Entity<MasterSiloComponent> ent, EntityUid item, EntityUid user)
    {
        if (TerminatingOrDeleted(item) ||
            !_compositionQuery.TryComp(item, out var composition))
            return false;

        if (!_power.IsPowered(ent.Owner))
        {
            _popup.PopupClient("It isn't powered!", ent, user);
            return false;
        }

        if (_net.IsClient)
            return true; // client wont have every silo in pvs range, but assume it succeeded

        if (Transform(ent).GridUid is not { } grid)
            return false; // should always exist if powered...

        if (!FindSilosAccepting(grid, (item, composition)))
        {
            _popup.PopupEntity("No powered silos on station!", ent, user);
            return false;
        }

        var multiplier = _stackQuery.CompOrNull(item)?.Count ?? 1;

        // now add material to all the silos
        PredictedQueueDel(item);
        var count = _silos.Count;
        foreach (var (material, volume) in composition.MaterialComposition)
        {
            // keep recycling the leftover material from division so it doesnt delete bits of materials
            var leftover = ent.Comp.Leftovers.GetValueOrDefault(material);
            var total = volume * multiplier + leftover;
            ent.Comp.Leftovers[material] = total % count;

            // now add the material to each silo
            var each = total / count;
            foreach (var silo in _silos)
            {
                _material.TryChangeMaterialAmount(silo, material, each, silo.Comp);
            }
        }
        _popup.PopupEntity($"Distributed {multiplier} {Name(item)} between {count} material silos", ent, user);
        return true;
    }

    private Entity<MasterSiloComponent>? FindValidMasterSilo(EntityUid grid, EntityUid item)
    {
        var query = EntityQueryEnumerator<MasterSiloComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.GridUid == grid && _power.IsPowered(uid) && SiloAccepts(comp, item))
                return (uid, comp);
        }

        return null;
    }

    private bool FindSilosAccepting(EntityUid grid, Entity<PhysicalCompositionComponent> item)
    {
        _silos.Clear();
        var query = EntityQueryEnumerator<MasterSiloClientComponent, MaterialStorageComponent, TransformComponent>();
        while (query.MoveNext(out var silo, out _, out var storage, out var xform))
        {
            if (xform.GridUid != grid || !_power.IsPowered(silo))
                continue;

            if (_whitelist.IsWhitelistFail(storage.Whitelist, item))
                continue;

            // inserting will fail and delete materials if this isnt checked now
            if (storage.MaterialWhiteList is { } whitelist)
            {
                var valid = true;
                foreach (var material in item.Comp.MaterialComposition.Keys)
                {
                    if (!whitelist.Contains(material))
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                    continue;
            }

            _silos.Add((silo, storage));
        }

        return _silos.Count > 0;
    }
}
