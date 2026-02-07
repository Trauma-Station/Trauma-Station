using Content.Server.Actions;
using Content.Server.Station.Systems;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Station.Components;
using Content.Shared.Throwing;
using Robust.Shared.Timing;

namespace Content.Server._DV.CosmicCult.EntitySystems;

public sealed class CosmicColossusSystem : SharedCosmicColossusSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicColossusComponent, ComponentInit>(OnSpawn);
    }

    private void OnSpawn(Entity<CosmicColossusComponent> ent, ref ComponentInit args) // I WANT THIS BIG GUY HURLED TOWARDS THE STATION
    {
        ent.Comp.DeathTimer = _timing.CurTime + ent.Comp.DeathWait;
        if (_station.GetStationInMap(Transform(ent).MapID) is { } station && TryComp<StationDataComponent>(station, out var stationData))
        {
            var stationGrid = _station.GetLargestGrid((station, stationData));
            _throw.TryThrow(ent, Transform(stationGrid!.Value).Coordinates, baseThrowSpeed: 30, null, 0, 0, false, false, false, false, false);
        }
        if (ent.Comp.Timed)
            _actions.AddAction(ent, ref ent.Comp.EffigyPlaceActionEntity, ent.Comp.EffigyPlaceAction, ent);
    }

    protected override void OnColossusEffigy(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusEffigy args)
    {
        if (!VerifyPlacement(ent, out var pos))
            return;

        _actions.RemoveAction(ent.Owner, ent.Comp.EffigyPlaceActionEntity);
        _codeCondition.SetCompleted(ent.Owner, ent.Comp.EffigyObjective); // Ts not predictable
        PredictedSpawnAtPosition(ent.Comp.EffigyPrototype, pos);
        ent.Comp.Timed = false;
    }

}
