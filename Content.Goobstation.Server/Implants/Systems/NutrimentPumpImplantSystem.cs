// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Implants.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Implants.Systems;

public sealed partial class NutrimentPumpImplantSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<NutrimentPumpImplantComponent, SubdermalImplantComponent>();
        while (query.MoveNext(out var uid, out var pump, out var implant))
        {
            if (now < pump.NextExecutionTime)
                continue;

            pump.NextExecutionTime = now + pump.ExecutionInterval;

            if (implant.ImplantedEntity is not { } mob ||
                !_satiationQuery.TryComp(mob, out var satiation))
                continue;

            var ent = new Entity<SatiationComponent>(mob, satiation);
            _satiation.ModifyValue(ent, SatiationSystem.Hunger, pump.FoodRate);
            _satiation.ModifyValue(ent, SatiationSystem.Thirst, pump.DrinkRate);
        }
    }
}
