using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Trauma - code relating to DNA freshness, GetSolution overriding and skills.
/// </summary>
public sealed partial class InjectorSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly CommonKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly EntProtoId FirstAidKnowledge = "FirstAidKnowledge";
    private static readonly DamageSpecifier NeedleDamage = new DamageSpecifier
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Brute", 10 }
        }
    };

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

    private void OnBeforeRangedInteract(Entity<InjectorComponent> injector, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        if (injector.Comp.InteractionRangeOverride is not { } range ||
            !_interaction.InRangeAndAccessible(args.User, target, range))
            return;

        if (HasComp<BloodstreamComponent>(target))
        {
            if (injector.Comp.IgnoreMobs)
            {
                _popup.PopupClient(Loc.GetString("injector-component-ignore-mobs"), target, args.User);
                return;
            }

            args.Handled |= TryMobsDoAfter(injector, args.User, target);
            return;
        }

        args.Handled |= TryContainerDoAfter(injector, args.User, target);
    }

    /// <summary>
    /// Runs the logic for checking and failing to inject someone due to low knowledge.
    /// </summary>
    private bool TryGetKnowledgeFirstAidFail(EntityUid user, EntityUid target)
    {
        if (!HasComp<KnowledgeHolderComponent>(user) || !HasComp<MobStateComponent>(target) || _mobState.IsDead(target))
            return false;

        var evFirstAid = new AddExperienceEvent(FirstAidKnowledge, 1);
        RaiseLocalEvent(user, ref evFirstAid);

        if (_knowledge.GetKnowledge(user, FirstAidKnowledge) is { } firstAid)
        {
            if (_knowledge.GetMastery(firstAid.Comp) > 1)
                return false;

            if (SharedRandomExtensions.PredictedProb(_timing, _knowledge.SharpCurve(firstAid, 0, 26), GetNetEntity(user)))
                return false;
        }

        var part = TargetBodyPart.Chest;
        if (TryComp<TargetingComponent>(user, out var targeting))
        {
            part = targeting.Target;
        }
        _damageable.TryChangeDamage(target, NeedleDamage, targetPart: part, origin: user);
        if (user == target)
        {
            _popup.PopupClient(Loc.GetString("injection-failed-self"), user, user);
            return true;
        }

        var uIdent = Identity.Entity(user, EntityManager);
        var tIdent = Identity.Entity(target, EntityManager);
        _popup.PopupClient(Loc.GetString("injection-failed-user", ("target", tIdent), ("part", part)), user, user);
        _popup.PopupEntity(Loc.GetString("injection-failed-target", ("user", uIdent), ("part", part)), target, target);
        return true;
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
