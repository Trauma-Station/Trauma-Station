using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Atmos;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitcode.Heretic.Rituals.EntityEffects;

public sealed class EnvironmentTemperatureConditionSystem : EntityConditionSystem<TemperatureTrackerComponent, EnvironmentTemperatureCondition>
{
    protected override void Condition(Entity<TemperatureTrackerComponent> entity,
        ref EntityConditionEvent<EnvironmentTemperatureCondition> args)
    {
        args.Result = entity.Comp.Temperature < args.Condition.Threshold;
    }
}

public sealed partial class EnvironmentTemperatureCondition : EntityConditionBase<EnvironmentTemperatureCondition>
{
    [DataField]
    public float Threshold = Atmospherics.T0C;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-environment-temperature",
            ("invert", Inverted),
            ("threshold", Threshold));
    }
}
