// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CombatMode;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class MeleeKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;


    private static readonly EntProtoId MeleeKnowledge = "MeleeKnowledge";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeExperience);
    }

    private void OnMeleeExperience(MeleeHitEvent args)
    {

        var xpMelee = 0;
        float weight = 0.0f;
        foreach (var hit in args.HitEntities)
        {
            if (args.User == hit)
                continue;

            if (TryComp<PhysicsComponent>(hit, out var comp))
                weight += comp.Mass;

            // Melee check to make sure we aren't just hitting walls or cuffed monkeys.
            if (!_mobState.IsAlive(hit) || !_combat.IsInCombatMode(hit) || !(TryComp<CuffableComponent>(hit, out var cuffs) && _cuffs.IsCuffed((hit, cuffs))))
                continue;
            xpMelee++;
        }

        var limit = 100;
        if (args.BaseDamage.GetTotal() <= 2)
            limit = 26;

        // send experience based on active combatants.
        var evMelee = new AddExperienceEvent(MeleeKnowledge, xpMelee, limit);
        RaiseLocalEvent(args.User, ref evMelee);

        // send experience based on weight.
        var evStrength = new AddExperienceEvent(MeleeKnowledge, Math.Min((int) (weight / 10), 10));
        RaiseLocalEvent(args.User, ref evStrength);
    }

    // Miss Event Hook
    private void OnAttackMiss(MeleeHitEvent args)
    {
        if (_knowledge.GetContainer(args.User) is not { } brain)
            return;

        if (_knowledge.GetKnowledge(brain, MeleeKnowledge) is not { } melee)
        {
            args.Handled = true;
            return;
        }

        if (_knowledge.GetMastery(melee.Comp) < 2 && SharedRandomExtensions.PredictedProb(_timing, 1 - _knowledge.SharpCurve(melee, 0, 26), GetNetEntity(args.User)))
        {
            args.Handled = true;
        }
    }
}
