using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class MeleeKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private readonly EntProtoId _meleeKnowledge = "MeleeKnowledge";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeExperience);
    }

    private void OnMeleeExperience(ref MeleeHitEvent args)
    {

        var xp = 0;
        foreach (var hit in args.HitEntities)
        {
            if (args.User == hit || !_mobState.IsAlive(hit))
                continue;
            xp++;
        }

        var limit = 100;
        if (args.BaseDamage.GetTotal() <= 2)
            limit = 26;

        var ev = new AddExperienceEvent(_meleeKnowledge, xp, limit);
        RaiseLocalEvent(args.User, ref ev);

        Log.Error($"{ToPrettyString(args.User)}");

        if (_knowledge.GetContainer(args.User) is not { } brain)
            return;

        if (_knowledge.GetKnowledge(brain, _meleeKnowledge) is not { } melee)
        {
            args.Handled = true;
            return;
        }
        Log.Error($"{ToPrettyString(args.User)} lcasdf");

        if (_knowledge.GetMastery(melee.Comp) < 2 && SharedRandomExtensions.PredictedProb(_timing, _knowledge.SharpCurve(melee, 0, 26), GetNetEntity(args.User)))
        {
            Log.Error($"{ToPrettyString(args.User)} aSS");
            args.Handled = true;
        }
    }
}
