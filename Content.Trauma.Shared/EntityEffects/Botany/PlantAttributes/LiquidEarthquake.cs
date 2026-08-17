// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects.Botany.PlantAttributes;

public sealed partial class LiquidEarthquake : EntityEffectBase<LiquidEarthquake>
{
    /// <summary>
    /// How low production can go.
    /// </summary>
    [DataField]
    public int ProductionLimit = 1;

    /// <summary>
    /// The decrease in production per effect application.
    /// </summary>
    [DataField]
    public double ProductionDecrease = 0.1;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-liquid-earthquake",
            ("limit", ProductionLimit),
            ("decrease", ProductionDecrease));
}
