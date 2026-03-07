// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CombatMode;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class MeleeKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;

    private static readonly EntProtoId MeleeKnowledge = "MeleeKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeSpeedKnowledgeComponent, GetMeleeAttackRateEvent>(OnGetMeleeAttackRate);
        SubscribeLocalEvent<MeleeDamageKnowledgeComponent, GetUserMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeExperience);
    }

    private void OnGetMeleeAttackRate(Entity<MeleeSpeedKnowledgeComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        args.Multipliers *= ent.Comp.Curve.GetCurve(level);
    }

    private void OnGetMeleeDamage(Entity<MeleeDamageKnowledgeComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        args.Damage *= ent.Comp.Curve.GetCurve(level);
    }

    // FIXME: wrong event to use bruh
    private void OnMeleeExperience(MeleeHitEvent args)
    {
        var user = args.User;
        if (!_knowledge.IsHolder(user))
            return;

        var xpMelee = 0;
        float weight = 0.0f;
        foreach (var hit in args.HitEntities)
        {
            if (user == hit)
                continue;

            if (TryComp<PhysicsComponent>(hit, out var comp))
                weight += comp.Mass;

            // Melee check to make sure we aren't just hitting walls or cuffed monkeys.
            if (!_mobState.IsAlive(hit) || !_combat.IsInCombatMode(hit) || !(TryComp<CuffableComponent>(hit, out var cuffs) && _cuffs.IsCuffed((hit, cuffs))))
                continue;
            xpMelee++;
        }

        var limit = args.BaseDamage.GetTotal().Int() switch
        {
            >= 50 => 100, // gonna have to get creative to master it
            >= 30 => 50,
            >= 20 => 26,
            >= 5 => 10,
            _ => 0
        };

        // give experience based on valid hit entities
        var evMelee = new AddExperienceEvent(MeleeKnowledge, xpMelee, limit);
        RaiseLocalEvent(user, ref evMelee);

        // give experience based on weight.
        var evStrength = new AddExperienceEvent(MeleeKnowledge, Math.Min((int) (weight / 10), 10), limit);
        RaiseLocalEvent(user, ref evStrength);
    }

    // Miss Event Hook
    /* this isnt implemented and melee miss should only be like 10% chance not guaranteed, its awful
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
    */
}
