// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Botany.PlantAttributes;

namespace Content.Trauma.Server.EntityEffects.Botany.PlantAttributes;

/// <summary>
/// This effect directly decreases the production of a PlantHolder's plant provided it exists and isn't dead.
/// Production correlates to how fast a plant produces once its fully matured, The lower the better
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
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
