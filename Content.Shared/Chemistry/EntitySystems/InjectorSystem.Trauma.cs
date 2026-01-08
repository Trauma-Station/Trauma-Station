using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Trauma - code relating to DNA freshness and GetSolution overriding.
/// </summary>
public sealed partial class InjectorSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Raises an event to allow other systems to modify where the injector's solution comes from.
    /// </summary>
    public Entity<SolutionComponent>? GetSolutionEnt(Entity<InjectorComponent> ent)
    {
        var ev = new InjectorGetSolutionEvent();
        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
            return ev.Solution;

        _solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution);
        return ent.Comp.Solution;
    }

    public Solution? GetSolution(Entity<InjectorComponent> ent)
        => GetSolutionEnt(ent)?.Comp.Solution;

    private void UpdateFreshness(Solution solution)
    {
        var now = _timing.CurTime;
        foreach (var dna in solution
            .SelectMany(r => r.Reagent.EnsureReagentData().OfType<DnaData>()))
        {
            dna.Freshness = now;
        }
    }
}

/// <summary>
/// Event raised on a hypospray before injecting/drawing to override what solution is used.
/// Overriding systems should set <c>Handled</c> to true and <c>Solution</c> to whatever solution.
/// </summary>
/// <remarks>
/// This can't be in common because it references SolutionComponent from Content.Shared
/// </remarks>
[ByRefEvent]
public record struct InjectorGetSolutionEvent(bool Handled = false, Entity<SolutionComponent>? Solution = null);
