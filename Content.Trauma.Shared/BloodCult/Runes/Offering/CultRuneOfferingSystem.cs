// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Bible.Components;
using Content.Shared.Cuffs;
using Content.Shared.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Stunnable;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.BloodCult.Runes.Revive;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Runes.Offering;

public sealed partial class CultRuneOfferingSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private CultRuneReviveSystem _runeRevive = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    private static readonly EntProtoId Muted = "StatusEffectMuted";
    private static readonly EntProtoId SoulShard = "SoulShard";

    [SubscribeLocalEvent]
    private void OnOfferingRuneInvoked(Entity<CultRuneOfferingComponent> ent, ref RuneInvokeEvent args)
    {
        var targets = _cult.GetTargetsNearRune(ent, ent.Comp.OfferingRange);
        targets.RemoveWhere(uid => _cult.IsCultist(uid));

        if (targets.Count == 0)
        {
            args.Popup = "There are no victims nearby";
            return;
        }

        var target = targets.First();
        var user = args.User;
        // if the target is dead we should always sacrifice it.
        if (_mob.IsDead(target))
        {
            Sacrifice(target, user);
            args.Handled = true;
            return;
        }

        var invokers = args.Invokers.Count;
        if (_mind.GetMind(target) == null ||
            _cult.IsTarget(user, target) ||
            HasComp<BibleUserComponent>(target) ||
            HasComp<MindShieldComponent>(target))
        {
            if (invokers < ent.Comp.AliveSacrificeInvokersAmount)
            {
                args.Popup = $"You need {ent.Comp.AliveSacrificeInvokersAmount} invokers to sacrifice a body";
                return;
            }

            Sacrifice(target, user);
        }
        else
        {
            if (invokers < ent.Comp.ConvertInvokersAmount)
            {
                args.Popup = $"You need {ent.Comp.ConvertInvokersAmount} invokers to convert a being";
                return;
            }

            Convert(ent, target, user);
        }

        _runeRevive.AddCharges(ent, ent.Comp.ReviveChargesPerOffering);
        args.Handled = true;
    }

    private void Sacrifice(EntityUid target, EntityUid user)
    {
        var pos = Transform(target).Coordinates;
        var shard = PredictedSpawnAtPosition(SoulShard, pos);
        _gibbing.Gib(target, user: user);

        var ev = new BloodCultSacrificedEvent(target, user);
        RaiseLocalEvent(ref ev);

        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return;

        _mind.TransferTo(mindId, shard, mind: mind);
        _mind.UnVisit(mindId);
    }

    private void Convert(Entity<CultRuneOfferingComponent> rune, EntityUid target, EntityUid user)
    {
        _cult.Convert(user, target);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(2f));
        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(2f));

        _cuffable.TryUncuff(target, user);

        _status.TryRemoveStatusEffect(target, Muted);
        _damage.ChangeDamage(target, rune.Comp.ConvertHealing, ignoreResistances: true);
    }
}

/// <summary>
/// Broadcast when a cultist sacrafices a mob.
/// </summary>
[ByRefEvent]
public record struct BloodCultSacrificedEvent(EntityUid Target, EntityUid User);
