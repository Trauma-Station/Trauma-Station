// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Station;
using Content.Shared.Station.Components;

namespace Content.Trauma.Shared.EntityEffects.Station;

/// <summary>
/// Station effect that queries all entities with a given component on the station, and applies some entity effects to them.
/// </summary>
public sealed partial class StationQueryEffects : EntityEffectBase<StationQueryEffects>
{
    /// <summary>
    /// Name of the component to query.
    /// </summary>
    [DataField(required: true)]
    public string CompName = string.Empty;

    /// <summary>
    /// The effects to apply to each entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Include paused entities in the query.
    /// Needed if you are running it on a pre-init map.
    /// </summary>
    [DataField]
    public bool IncludePaused;

    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}

public sealed class StationQueryEffectsSystem : EntityEffectSystem<StationDataComponent, StationQueryEffects>
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    protected override void Effect(Entity<StationDataComponent> ent, ref EntityEffectEvent<StationQueryEffects> args)
    {
        var e = args.Effect;
        var type = Factory.GetRegistration(e.CompName).Type;
        var effects = e.Effects;

        var station = ent.Owner;
        foreach (var (uid, _) in EntityManager.GetAllComponents(type, e.IncludePaused))
        {
            if (_station.GetOwningStation(uid) != station)
                continue;

            _effects.ApplyEffects(uid, effects);
        }
    }
}
