using Content.Shared.Chemistry.Components;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Trauma - add GetSolution which raises an event to allow other systems to modify where the hypospray's solution comes from.
/// </summary>
public sealed partial class HypospraySystem
{
    public Entity<SolutionComponent>? GetSolution(Entity<HyposprayComponent> ent)
    {
        var ev = new HyposprayGetSolutionEvent();
        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
            return ev.Solution;

        _solutionContainers.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var solution);
        return solution;
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
public record struct HyposprayGetSolutionEvent(bool Handled = false, Entity<SolutionComponent>? Solution = null);
