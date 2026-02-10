using Content.Lavaland.Shared.EntityConditions;
using Content.Lavaland.Shared.Procedural.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.EntityConditions;

namespace Content.Lavaland.Server.EntityConditions;

public sealed class PressureConditionSystem : EntityConditionSystem<TransformComponent, PressureCondition>
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    protected override void Condition(Entity<TransformComponent> ent, ref EntityConditionEvent<PressureCondition> args)
    {
        if (args.Condition.WorksOnLavaland && HasComp<LavalandMapComponent>(ent.Comp.MapUid))
        {
            args.Result = true;
            return;
        }

        var mix = _atmos.GetTileMixture((ent, ent.Comp));
        var pressure = mix?.Pressure ?? 0f;
        var min = args.Condition.Min;
        var max = args.Condition.Max;
        args.Result = pressure >= min && pressure <= max;
    }
}
