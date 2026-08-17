using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Botany;

namespace Content.Trauma.Server.EntityEffects.Botany;

public sealed partial class LiquidEarthquakeEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, LiquidEarthquake>
{
    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<LiquidEarthquake> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        if (entity.Comp.Seed.Production > args.Effect.ProductionLimit)
        {
            entity.Comp.Seed.Production = (float)Math.Max(entity.Comp.Seed.Production - args.Effect.ProductionDecrease, args.Effect.ProductionLimit);
        }
    }
}
