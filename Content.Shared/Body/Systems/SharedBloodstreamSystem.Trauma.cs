using Content.Goobstation.Common.CCVar;
using Content.Shared.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

/// <summary>
/// Trauma - Provides missing API methods for bloodstream.
/// </summary>
public abstract partial class SharedBloodstreamSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _bloodlossMultiplier = 4f;

    private void InitializeTrauma()
    {
        Subs.CVar(_cfg, GoobCVars.BleedMultiplier, value => _bloodlossMultiplier = value, true);
    }

    public void SetRefreshAmount(Entity<BloodstreamComponent> ent, FixedPoint2 amount)
    {
        ent.Comp.BloodRefreshAmount = amount;
        DirtyField(ent.AsNullable(), nameof(BloodstreamComponent.BloodRefreshAmount));
    }

    /// <summary>
    /// Removes a certain amount of all reagents except of excluded ones from the bloodstream.
    /// </summary>
    public Solution? FlushChemicals(Entity<BloodstreamComponent?> ent,
        FixedPoint2 quantity,
        params ProtoId<ReagentPrototype>[] excludedReagents)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return null;

        var flushedSolution = new Solution();

        for (var i = bloodSolution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagentId, _) = bloodSolution.Contents[i];
            if (ent.Comp.BloodReferenceSolution.ContainsPrototype(reagentId.Prototype) ||
                excludedReagents.Contains(reagentId.Prototype))
                continue;

            var reagentFlushAmount = SolutionContainer.RemoveReagent(ent.Comp.BloodSolution.Value, reagentId, quantity);
            flushedSolution.AddReagent(reagentId, reagentFlushAmount);
        }

        return flushedSolution.Volume == 0 ? null : flushedSolution;
    }

}
