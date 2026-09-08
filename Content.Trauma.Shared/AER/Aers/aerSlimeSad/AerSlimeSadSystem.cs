using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;

namespace Content.Trauma.Shared.AER;

public sealed partial class AerSlimeSadSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    /// <summary>
    /// raises the research and id gear event on the horse wailing
    /// </summary>
    [SubscribeLocalEvent]
    private void OnGloomyFrown(Entity<AerSlimeSadComponent> ent, ref GloomyFrownEvent args)
    {
        if (!TryUseAbility(args))
            return;

        var range = ent.Comp.Range;
        var source = ent.Owner;
        var reagents = ent.Comp.Reagents;
        var targets = new HashSet<Entity<BloodstreamComponent>>();
        _lookup.GetEntitiesInRange(Transform(source).Coordinates, range, targets);

        var victimCounter = 0;
        foreach (var target in targets)
        {
            //dont inject dead or yourself
            if (target.Owner == source
                || _mobState.IsDead(target))
                continue;

            if (TryInjectReagents(target, reagents))
                victimCounter++;
        }

        //var spawnEvent = new AerBehaviourSpawnGearEvent(ent.Owner);
        //RaiseLocalEvent(ent.Owner, ref spawnEvent);
        var researchEvent = new AerBehaviourAddResearchEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref researchEvent);
    }

    public bool TryInjectReagents(EntityUid uid, Dictionary<string, FixedPoint2> reagents)
    {
        var solution = new Solution();
        foreach (var reagent in reagents)
            solution.AddReagent(reagent.Key, reagent.Value);

        if (!_solution.TryGetInjectableSolution(uid, out var targetSolution, out var _))
            return false;

        if (!_solution.TryAddSolution(targetSolution.Value, solution))
            return false;

        return true;
    }

    private static bool TryUseAbility(BaseActionEvent action)
    {
        if (action.Handled)
            return false;

        action.Handled = true;
        return true;
    }
}