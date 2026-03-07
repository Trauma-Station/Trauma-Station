// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    private static readonly EntProtoId FirstAidKnowledge = "FirstAidKnowledge";
    public void InitializeInjector()
    {
        SubscribeLocalEvent<InjectorComponent, InjectorBeforeInjectEvent>(OnInjectorCheck);
    }

    private void OnInjectorCheck(Entity<InjectorComponent> ent, ref InjectorBeforeInjectEvent args)
    {
        var user = args.EntityUsingInjector;
        var target = args.TargetGettingInjected;
        // This codes only gonna run if the target is not dead and the user has a knowledge component and is not using something like a medipen.
        if (HasComp<EasyToUseComponent>(ent) || !HasComp<KnowledgeHolderComponent>(user) || !HasComp<MobStateComponent>(target) || _mobState.IsDead(target) || GetContainer(user) is not { } brain)
            return;

        var evFirstAid = new AddExperienceEvent(FirstAidKnowledge, 1);
        RaiseLocalEvent(user, ref evFirstAid);

        if (GetKnowledge(brain, FirstAidKnowledge) is { } firstAid)
        {
            // No need to roll a random number if we're average in first aid. It's trivial for the user.
            if (GetMastery(firstAid.Comp) > 2)
                return;

            if (SharedRandomExtensions.PredictedProb(_timing, SharpCurve(firstAid, 0, 26), GetNetEntity(user)))
                return;
        }

        var part = TargetBodyPart.Chest;
        if (TryComp<TargetingComponent>(user, out var targeting))
        {
            part = targeting.Target;
        }

        if (user == target)
        {
            args.OverrideMessage = Loc.GetString("injection-failed-self", ("target", target), ("user", user), ("part", part));
        }
        else
        {
            args.OverrideMessage = Loc.GetString("injection-failed-user", ("target", target), ("user", user), ("part", part));
            _popup.PopupClient(Loc.GetString("injection-failed-target", ("target", target), ("user", user), ("part", part)), target, target);
        }
        args.Cancel();
    }
}
