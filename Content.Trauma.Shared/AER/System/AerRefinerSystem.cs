// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Damage.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Examine;
using Content.Shared.Ghost.Roles;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Gibbing;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// This handles logic relating to <see cref="AerRefinerComponent"/>
/// </summary>
public sealed partial class AerRefinerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GibbingSystem _gibbing = default!;

    /// <inheritdoc/>
    [SubscribeLocalEvent]
    private void OnInit(Entity<AerRefinerComponent> ent, ref ComponentInit args)
    {
        //ent.Comp.OutputContainer = _containerSystem.EnsureContainer<Container>(ent, ent.Comp.OutputContainerName);
    }

    [SubscribeLocalEvent]
    private void OnStorageAfterOpen(Entity<AerRefinerComponent> ent, ref StorageAfterOpenEvent args)
    {
        StopRefining(ent);
        //_containerSystem.EmptyContainer(ent.Comp.OutputContainer);
    }

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<AerRefinerComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;


        args.Handled = true;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<AerRefinerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || ent.Comp.Refining)
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var entityStorageComp) ||
            entityStorageComp.Contents.ContainedEntities.Count == 0)
            return;

        if (!_power.IsPowered(ent.Owner))
            return;

        var user = args.User;
        var verb = new AlternativeVerb
        {
            Text = "Activate",
            Priority = 2,
            Act = () => StartRefining((ent, ent.Comp, entityStorageComp), user)
        };
        args.Verbs.Add(verb);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<AerRefinerComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            StopRefining(ent);
    }

    public void StartRefining(Entity<AerRefinerComponent, EntityStorageComponent> ent, EntityUid? user = null)
    {
        var (uid, refiner, _) = ent;

        if (refiner.Refining)
            return;


        refiner.Refining = true;
        refiner.RefineEndTime = _timing.CurTime + refiner.RefineDuration;
        refiner.RefiningSoundEntity = _audioSystem.PlayPredicted(refiner.RefiningSound, ent, user)?.Entity ?? refiner.RefiningSoundEntity;
        _appearance.SetData(ent, AerRefinerVisuals.Refining, true);
        Dirty(ent, ent.Comp1);
    }

    public void StopRefining(Entity<AerRefinerComponent> ent, bool early = true)
    {
        if (!ent.Comp.Refining)
            return;

        ent.Comp.Refining = false;
        _appearance.SetData(ent, AerRefinerVisuals.Refining, false);

        if (early)
            ent.Comp.RefiningSoundEntity = _audioSystem.Stop(ent.Comp.RefiningSoundEntity);

        Dirty(ent, ent.Comp);
    }

    public void FinishRefining(Entity<AerRefinerComponent, EntityStorageComponent> ent)
    {
        var (_, refiner, storage) = ent;
        StopRefining((ent, ent.Comp1), false);
        _audioSystem.PlayPredicted(refiner.RefiningCompleteSound, ent, null);
        refiner.RefiningSoundEntity = null;
        Dirty(ent, ent.Comp1);

        var contents = new ValueList<EntityUid>(storage.Contents.ContainedEntities);
        var coords = Transform(ent).Coordinates;
        foreach (var contained in contents)
        {
            if (_whitelistSystem.IsWhitelistPass(refiner.RefiningWhitelist, contained))
            {
                if (TryComp<ActivatedAerCoreComponent>(contained, out var coreComponent) && coreComponent.ActiveCore == false)
                {
                    if (ChooseWeightedUnique((ent.Owner, refiner), out var ghostroleSpawn))
                    {
                        coreComponent.ActiveCore = true;
                        var deleteOnTrigger = EnsureComp<DeleteOnTriggerComponent>(contained);
                        deleteOnTrigger.KeysIn.Remove("trigger");
                        deleteOnTrigger.KeysIn.Add("timer");
                        var spawnOnTrigger = EnsureComp<SpawnOnTriggerComponent>(contained);
                        spawnOnTrigger.KeysIn.Remove("trigger");
                        spawnOnTrigger.KeysIn.Add("timer");
                        spawnOnTrigger.Proto = ghostroleSpawn;
                        spawnOnTrigger.Predicted = true;
                    }
                }
            }
        }
    }

    public bool ChooseWeightedUnique(Entity<AerRefinerComponent> ent, out EntProtoId protoId)
    {
        var spawnlist = ent.Comp.AvailableSpawn;
        double totalWeight = spawnlist.Sum(x => x.Item2);
        double roll = _random.NextDouble() * totalWeight;

        foreach (var entry in spawnlist)
        {
            if (entry.weight <= 0)
                continue;

            roll -= entry.weight;

            if (roll < 0)
            {
                protoId = entry.spawn;
                ent.Comp.AvailableSpawn.Remove(entry);
                Dirty(ent);
                return true;
            }
        }
        protoId = default;
        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AerRefinerComponent, EntityStorageComponent>();
        while (query.MoveNext(out var uid, out var refiner, out var storage))
        {
            if (!refiner.Refining)
                continue;

            if (refiner.RefineEndTime < _timing.CurTime)
                FinishRefining((uid, refiner, storage));
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<AerRefinerComponent> aerRefiner, ref MapInitEvent args)
    {
        if (aerRefiner.Comp.SpawnTable is not { } table)
            return;

        aerRefiner.Comp.AvailableSpawn = _entityTable.ListSpawns(table).ToList();
    }
}
