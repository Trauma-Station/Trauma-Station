// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Silicons.Laws;
using Content.Shared.EntityEffects;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Content.Trauma.Shared.EntityEffects.Station;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Server.EntityEffects.Station;

public sealed class StationAiRandomLawsetSystem : EntityEffectSystem<StationDataComponent, StationAiRandomLawset>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SiliconLawSystem _law = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    protected override void Effect(Entity<StationDataComponent> ent, ref EntityEffectEvent<StationAiRandomLawset> args)
    {
        var lawset = _random.Pick(args.Effect.Lawsets);
        var query = EntityQueryEnumerator<StationAiCustomizationComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_station.GetStation(uid) != ent.Owner)
                continue;

            var laws = _law.GetLawset(lawset).Laws;
            _law.SetLaws(laws, uid);
        }
    }
}
