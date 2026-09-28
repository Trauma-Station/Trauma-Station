// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Bible.Components;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mindshield;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Trauma.Shared.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult.Runes.Revive;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Runes.Offering;

public sealed partial class CultRuneOfferingSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private CultRuneReviveSystem _runeRevive = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    private static readonly EntProtoId Dagger = "RitualDagger";
    private static readonly EntProtoId Muted = "StatusEffectMuted";
    private static readonly EntProtoId SoulShard = "SoulShard";

    [SubscribeLocalEvent]
    private void OnOfferingRuneInvoked(Entity<CultRuneOfferingComponent> ent, ref RuneInvokeEvent args)
    {
        var user = args.User;
        if (_cult.GetRule(user) is not { } rule)
            return;

        var targets = _cult.GetTargetsNearRune(ent, ent.Comp.OfferingRange);
        targets.RemoveWhere(uid => _cult.IsCultist(uid));

        if (targets.Count == 0)
        {
            args.Popup = "There are no victims nearby";
            return;
        }

        var target = targets.First();
        // if the target is dead we should always sacrifice it.
        if (_mob.IsDead(target))
        {
            Sacrifice(rule, target, user);
            args.Handled = true;
            return;
        }

        var invokers = args.Invokers.Count;
        if (_mind.GetMind(target) == null ||
            target == rule.Comp.OfferingTarget ||
            HasComp<BibleUserComponent>(target) ||
            _mindShield.IsShielded(target))
        {
            if (invokers < ent.Comp.AliveSacrificeInvokersAmount)
            {
                args.Popup = $"You need {ent.Comp.AliveSacrificeInvokersAmount} invokers to sacrifice a body";
                return;
            }

            Sacrifice(rule, target, user);
        }
        else
        {
            if (invokers < ent.Comp.ConvertInvokersAmount)
            {
                args.Popup = $"You need {ent.Comp.ConvertInvokersAmount} invokers to convert a being";
                return;
            }

            Convert(ent, rule, target, user);
        }

        args.Handled = true;
    }

    private void Sacrifice(Entity<BloodCultRuleComponent> rule, EntityUid target, EntityUid user)
    {
        var hasMind = _mind.TryGetMind(target, out var mindId, out var mind);

        var pos = Transform(target).Coordinates;
        _gibbing.Gib(target, user: user);

        var ev = new BloodCultSacrificedEvent(rule, target, user);
        RaiseLocalEvent(ref ev);

        if (!hasMind)
            return;

        var shard = PredictedSpawnAtPosition(SoulShard, pos);
        _mind.TransferTo(mindId, shard, mind: mind);
        _mind.UnVisit(mindId);

        _runeRevive.AddCharges(rule, 1);
    }

    private void Convert(Entity<CultRuneOfferingComponent> rune, EntityUid rule, EntityUid target, EntityUid user)
    {
        _cult.Convert(rule, target);

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(2f));
        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(2f));

        _cuffable.TryUncuff(target, user);

        _status.TryRemoveStatusEffect(target, Muted);
        _damage.ChangeDamage(target, rune.Comp.ConvertHealing, ignoreResistances: true,
            targetPart: TargetBodyPart.All, canMiss: false, splitDamage: SplitDamageBehavior.None);

        // free
        var dagger = PredictedSpawnAtPosition(Dagger, Transform(rune).Coordinates);
        _hands.TryPickupAnyHand(target, dagger);
    }
}

/// <summary>
/// Broadcast when a cultist sacrifices a mob.
/// </summary>
[ByRefEvent]
public record struct BloodCultSacrificedEvent(Entity<BloodCultRuleComponent> Rule, EntityUid Target, EntityUid User);
