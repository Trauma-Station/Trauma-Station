// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee;

namespace Content.Goobstation.Shared.Blob;

public sealed partial class BlobFactorySystem : EntitySystem
{
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private EntityQuery<BlobCoreComponent> _coreQuery = default!;
    [Dependency] private EntityQuery<BlobTileComponent> _tileQuery = default!;

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<BlobFactoryComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Blobbernaut is not { } mob || TerminatingOrDeleted(mob))
            return;

        if (!TryComp<BlobbernautComponent>(mob, out var comp))
            return;

        DebugTools.Assert(comp.Factory == ent.Owner, $"{ToPrettyString(mob)} had wrong factory {ToPrettyString(comp.Factory)}, expected {ToPrettyString(ent)}");
        comp.Factory = null;
    }

    public bool ProduceBlobbernaut(Entity<BlobFactoryComponent> ent)
    {
        if (ent.Comp.HasBlobbernaut ||
            !_tileQuery.TryComp(ent, out var tile) ||
            !_coreQuery.TryComp(tile.Core, out var core))
            return false;

        var coords = Transform(ent).Coordinates;
        var mob = PredictedSpawnAtPosition(ent.Comp.BlobbernautId, coords);
        ent.Comp.Blobbernaut = mob;
        ent.Comp.HasBlobbernaut = true;
        Dirty(ent);

        var chem = ProtoMan.Index(core.CurrentChem);
        if (TryComp<BlobbernautComponent>(mob, out var comp))
        {
            comp.Factory = ent;
            comp.CurrentChem = core.CurrentChem;
            Dirty(mob, comp);
        }
        if (TryComp<MeleeWeaponComponent>(mob, out var melee))
        {
            melee.Damage = chem.Damage * 0.8f;
            Dirty(mob, melee);
        }
        return true;
    }

    private void FillSmokeGas(Entity<BlobPodComponent> ent, ProtoId<BlobChemPrototype> id)
    {
        var chem = ProtoMan.Index(id);
        var blobGas = EnsureComp<SmokeOnTriggerComponent>(ent);
        blobGas.Solution = chem.SmokeSolution; // hopefully nothing ever changes this :D
        Dirty(ent, blobGas);
    }

    [SubscribeLocalEvent]
    private void OnPulse(Entity<BlobFactoryComponent> ent, ref BlobSpecialPulseEvent args)
    {
        if (!_tileQuery.TryComp(ent, out var tile) ||
            tile.Core is not { } core ||
            !_coreQuery.TryComp(core, out var coreComp))
            return;

        // forget dead pods
        var oldPodCount = ent.Comp.BlobPods.Count;
        ent.Comp.BlobPods.RemoveAll(b => TerminatingOrDeleted(b) || !_mob.IsAlive(b));
        if (oldPodCount != ent.Comp.BlobPods.Count)
            Dirty(ent);

        if (ent.Comp.BlobPods.Count >= ent.Comp.SpawnLimit)
            return;

        if (ent.Comp.Accumulator < ent.Comp.AccumulateToSpawn)
        {
            ent.Comp.Accumulator++;
            return;
        }

        var xform = Transform(ent);

        var pod = PredictedSpawnAtPosition(ent.Comp.Pod, xform.Coordinates);
        ent.Comp.BlobPods.Add(pod);
        Dirty(ent);
        var blobPod = EnsureComp<BlobPodComponent>(pod);
        blobPod.Core = core;
        FillSmokeGas((pod, blobPod), coreComp.CurrentChem);

        ent.Comp.Accumulator = 0;
    }
}
