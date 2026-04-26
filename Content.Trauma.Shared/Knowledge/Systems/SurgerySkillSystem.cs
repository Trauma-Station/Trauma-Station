// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Surgery;
using Content.Shared.DoAfter;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed class SurgerySkillSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedSurgerySystem _surgery = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly EntProtoId SurgeryKnowledge = "SurgeryKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryTargetComponent, DoAfterAttemptEvent<SurgeryDoAfterEvent>>(OnBeforeTargetDoAfter);
    }

    private void OnBeforeTargetDoAfter(Entity<SurgeryTargetComponent> ent, ref DoAfterAttemptEvent<SurgeryDoAfterEvent> args)
    {
        if (_net.IsClient
            || !args.Event.Repeat) // We only wanna do this laggy shit on repeatables. One-time stuff idc.
            return;

        if (args.Event.Target is not { } target || !_surgery.IsSurgeryValid(ent, target, args.Event.Surgery, args.Event.User, out _, out _))
        {
            args.Cancel();
            return;
        }

        // Time to skill roll
        if (_surgery.GetSingleton(args.Event.Surgery) is not { } surgery || _knowledge.GetContainer(args.Event.User) is not { } brain || _knowledge.GetKnowledge(brain, SurgeryKnowledge) is not { } skill)
            return;

        var complexity = surgery.Complexity;
        var time = (_timing.CurTime - args.DoAfter.StartTime).Seconds - 10; // Increases surgery complexity the longer it takes
        var mastery = _knowledge.GetMastery(skill.Comp);

        if (mastery >= 5) // Half the complexity if you're a master.
            time /= 2;

        if (time > 0.0)
            complexity += time;

        // Needs the implementation PR
        /*
        var ev = new SingleContestEvent(100, complexity, skill.Comp.NetLevel, true);
        RaiseLocalEvent(args.Event.User, ref ev);

        if (ev.Failed)
        */

        // Purely random atm on time atm
        if (SharedRandomExtensions.PredictedProb(_timing, Math.Min(1 - time / 10, 0), GetNetEntity(ent)))
        {
            args.Cancel();
            _knowledge.AddExperience(brain, SurgeryKnowledge, Math.Max(complexity / 5, 5)); // Give experience to a failed surgery based on the complexity of the thing. At least you tried.
        }

        // Give experience to the surgeon based upon how hard the surgery was.
        if (mastery >= 3)
            _knowledge.AddExperience(brain, SurgeryKnowledge, Math.Max(complexity, 0)); // You're gonna need to challenge yourself if you want to improve. Try surgery on yourself, standing up.
        else
            _knowledge.AddExperience(brain, SurgeryKnowledge, Math.Max(complexity + (mastery - 2) * 3, (mastery - 2) * 3)); // Nothing beats practice to learn something.
    }
}
