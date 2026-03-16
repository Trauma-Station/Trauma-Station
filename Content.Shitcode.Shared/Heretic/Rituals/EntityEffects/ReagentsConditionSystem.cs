using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitcode.Heretic.Rituals.EntityEffects;

public sealed class ReagentsEntityConditionSystem : EntityConditionSystem<PuddleComponent, ReagentsCondition>
{
    [Dependency] private readonly SharedSolutionContainerSystem _sol = default!;


    protected override void Condition(Entity<PuddleComponent> entity,
        ref EntityConditionEvent<ReagentsCondition> args)
    {
        if (!_sol.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out _, out var sol))
            return;

        var quant = sol.GetTotalPrototypeQuantity(args.Condition.Reagents);

        args.Result = quant > args.Condition.Min && quant < args.Condition.Max;
    }
}

public sealed partial class ReagentsCondition : EntityConditionBase<ReagentsCondition>
{
    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public ProtoId<ReagentPrototype>[] Reagents =
    [
        "Blood",
        "AmmoniaBlood",
        "InsectBlood",
        "CopperBlood",
        "ZombieBlood",
        "AlienBlood",
        "BlackBlood",
        "BloodChangeling",
    ];

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        if (!prototype.Resolve(Reagents[0], out var reagentProto))
            return string.Empty;

        return Loc.GetString("entity-condition-guidebook-reagent-threshold",
            ("reagent", reagentProto.LocalizedName),
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }
}
