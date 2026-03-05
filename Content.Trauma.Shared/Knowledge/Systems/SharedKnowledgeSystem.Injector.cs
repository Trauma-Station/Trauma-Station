using Content.Medical.Common.Targeting;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    private static readonly EntProtoId FirstAidKnowledge = "FirstAidKnowledge";
    private static readonly DamageSpecifier NeedleDamage = new DamageSpecifier
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Brute", 10 }
        }
    };

    public void InitializeInjector()
    {
        SubscribeLocalEvent<InjectorComponent, >(OnInjectorCheck);
    }

    private void OnInjectorCheck(Entity<InjectorComponent> ent, ref args)
    {
        // This codes only gonna run if the target is not dead and the user has a knowledge component and is not using something like a medipen.
        if (HasComp<EasyToUseComponent>(ent) || !HasComp<KnowledgeHolderComponent>(user) || !HasComp<MobStateComponent>(target) || _mobState.IsDead(target))
            return false;

        var evFirstAid = new AddExperienceEvent(FirstAidKnowledge, 1);
        RaiseLocalEvent(user, ref evFirstAid);

        if (TryGetKnowledgeUnit(user, FirstAidKnowledge) is { } firstAid)
        {
            // No need to roll a random number if we're average in first aid. It's trivial for the user.
            if (GetMastery(firstAid) > 2)
                return;

            if (SharedRandomExtensions.PredictedProb(_timing, SharpCurve(firstAid, 0, 26), GetNetEntity(user)))
                return;
        }

        var part = TargetBodyPart.Chest;
        if (TryComp<TargetingComponent>(user, out var targeting))
        {
            part = targeting.Target;
        }

        _damageable.TryChangeDamage(target, NeedleDamage, targetPart: part, origin: user);
        if (user == target)
        {
            _popup.PopupClient(Loc.GetString("injection-failed-self", ("target", target), ("user", user), ("part", part)), user, user);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("injection-failed-user", ("target", target), ("user", user), ("part", part)), user, user);
            _popup.PopupClient(Loc.GetString("injection-failed-target", ("target", target), ("user", user), ("part", part)), target, target);
        }
        args.Miss = true;
    }
}
