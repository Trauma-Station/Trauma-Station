using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Botany;

namespace Content.Trauma.Server.EntityEffects.Botany;

public sealed partial class PlantAdjustMaturationEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantAdjustMaturation>
{
    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantAdjustMaturation> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        entity.Comp.Seed.Maturation = Math.Max(entity.Comp.Seed.Maturation + args.Effect.Amount, 1);
    }
}
