// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects.Station;

/// <summary>
/// Station effect that finds an unblocked area with a given component on the station, then applies some entity effects to it.
/// </summary>
public sealed partial class StationAreaEffects : EntityEffectBase<StationAreaEffects>
{
    /// <summary>
    /// Name of the area's component to query.
    /// </summary>
    [DataField(required: true)]
    public string AreaComp = string.Empty;

    /// <summary>
    /// The effects to apply to the chosen area.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [DataField]
    public int Min = 1;

    [DataField]
    public int Max = 1;

    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}

public sealed class StationAreaEffectsSystem : EntityEffectSystem<StationDataComponent, StationAreaEffects>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private List<EntityUid> _areas = new();

    protected override void Effect(Entity<StationDataComponent> ent, ref EntityEffectEvent<StationAreaEffects> args)
    {
        var type = Factory.GetRegistration(args.Effect.AreaComp).Type;

        // TODO: make a open areas cache somewhere...
        var station = ent.Owner;
        var mask = CollisionGroup.MobMask;
        _areas.Clear();
        foreach (var (uid, _) in EntityManager.GetAllComponents(type))
        {
            if (_station.GetOwningStation(uid) != station)
                continue;

            var coords = Transform(uid).Coordinates;
            if (_turf.GetTileRef(coords) is not {} tile || _turf.IsTileBlocked(tile, mask))
                continue;

            _areas.Add(uid);
        }

        var count = _random.Next(args.Effect.Min, args.Effect.Max);
        for (int i = 0; i < count; i++)
        {
            if (_areas.Count == 0)
                return;

            var area = _random.PickAndTake(_areas);
            _effects.ApplyEffects(area, args.Effect.Effects);
        }
    }
}
