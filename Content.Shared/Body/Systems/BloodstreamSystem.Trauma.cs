// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Shared.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

/// <summary>
/// Trauma - Provides missing API methods for bloodstream.
/// </summary>
public sealed partial class BloodstreamSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _query = default!;

    private float _bloodlossMultiplier = 4f;

    public override void Initialize()
    {
        base.Initialize();

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
        if (!_query.Resolve(ent, ref ent.Comp, logMissing: false)
            || !_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return null;

        var flushedSolution = new Solution();

        for (var i = bloodSolution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagentId, _) = bloodSolution.Contents[i];
            if (ent.Comp.BloodReferenceSolution.ContainsPrototype(reagentId.Prototype) ||
                excludedReagents.Contains(reagentId.Prototype))
                continue;

            var reagentFlushAmount = _solutionContainer.RemoveReagent(ent.Comp.BloodSolution.Value, reagentId, quantity);
            flushedSolution.AddReagent(reagentId, reagentFlushAmount);
        }

        return flushedSolution.Volume == 0 ? null : flushedSolution;
    }

}
