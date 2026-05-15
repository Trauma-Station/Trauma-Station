// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Areas;
using Content.Trauma.Shared.EntityEffects;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class TeleportRandomAreaSystem : EntityEffectSystem<TransformComponent, TeleportRandomArea>
{
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    public const int Oxygen = (int) Gas.Oxygen;

    private List<Entity<TransformComponent>> _areas = new();

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<TeleportRandomArea> args)
    {
        var map = ent.Comp.MapID;
        // TODO: add area teleport blacklist check if its needed for anything in the future
        Predicate<Entity<TransformComponent>> pred = args.Effect.Safe
            ? _ => true
            : IsTileUnsafe;
        _areas.Clear();
        _area.AddOpenAreas(map, _areas, pred);
        if (_areas.Count == 0)
            return;

        var area = _random.PickAndTake(_areas);
        // TODO: add poof effects
        _teleport.Teleport(ent.Owner, area.Comp.Coordinates);
    }

    private bool IsTileUnsafe(Entity<TransformComponent> area)
        => _atmos.GetTileMixture(area.AsNullable()) is not {} mixture || // space
            mixture.Temperature <= 270 || mixture.Temperature >= 360 || // bad temp
            mixture.Pressure <= 20 || mixture.Pressure >= 300 || // bad pressure
            mixture[Oxygen] < 16; // not enough oxygen
}
