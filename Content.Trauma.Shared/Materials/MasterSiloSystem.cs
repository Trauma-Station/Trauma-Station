// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Materials.OreSilo;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;

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
    }

    private void OnInteractUsing(Entity<MasterSiloComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        var item = args.Used;
        if (TerminatingOrDeleted(item) ||
            !_compositionQuery.TryComp(item, out var composition) ||
            !_whitelist.CheckBoth(item, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        args.Handled = true;
        if (!_power.IsPowered(ent.Owner))
        {
            _popup.PopupClient("It isn't powered!", ent, user);
            return;
        }

        if (_net.IsClient)
            return; // client wont have every silo in pvs range

        if (Transform(ent).GridUid is not { } grid)
            return; // should always exist if powered...

        if (!FindSilosAccepting(grid, item))
        {
            _popup.PopupEntity("No powered silos on station!", ent, user);
            return;
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
    }

    private bool FindSilosAccepting(EntityUid grid, EntityUid item)
    {
        _silos.Clear();
        var query = EntityQueryEnumerator<OreSiloComponent, MaterialStorageComponent, TransformComponent>();
        while (query.MoveNext(out var silo, out _, out var storage, out var xform))
        {
            if (xform.GridUid != grid || !_power.IsPowered(silo))
                continue;

            if (_whitelist.IsWhitelistFail(storage.Whitelist, item))
                continue;

            _silos.Add((silo, storage));
        }

        return _silos.Count > 0;
    }
}
