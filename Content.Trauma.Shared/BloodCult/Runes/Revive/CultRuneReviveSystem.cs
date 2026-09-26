// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Shared.BloodCult.Gamerule;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Runes.Revive;

public abstract partial class CultRuneReviveSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] protected ISharedPlayerManager Player = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] protected SharedMindSystem Mind = default!;

    [SubscribeLocalEvent]
    private void OnReviveRuneInvoked(Entity<CultRuneReviveComponent> ent, ref RuneInvokeEvent args)
    {
        var cost = ent.Comp.ChargesUsed;
        if (_cult.GetRule(args.User) is not { } rule || rule.Comp.ReviveCharges < cost)
        {
            args.Popup = Loc.GetString("cult-revive-rune-no-charges");
            return;
        }

        var targets = _cult.GetTargetsNearRune(ent, ent.Comp.ReviveRange);
        targets.RemoveWhere(uid =>
            !HasComp<DamageableComponent>(uid) ||
            !HasComp<MobThresholdsComponent>(uid) ||
            !HasComp<MobStateComponent>(uid) ||
            _mob.IsAlive(uid));

        if (targets.Count == 0)
        {
            args.Popup = Loc.GetString("cult-rune-no-targets");
            return;
        }

        var victim = targets.First();

        Revive(rule, victim, args.User, ent);
        args.Handled = true;
    }

    public void AddCharges(Entity<BloodCultRuleComponent> rule, int charges)
    {
        rule.Comp.ReviveCharges += charges;
        DirtyField(rule, rule.Comp, nameof(BloodCultRuleComponent.ReviveCharges));
    }

    private void Revive(Entity<BloodCultRuleComponent> rule, EntityUid target, EntityUid user, Entity<CultRuneReviveComponent> rune)
    {
        AddCharges(rule, -rune.Comp.ChargesUsed);

        var deadThreshold = _threshold.GetThresholdForState(target, MobState.Dead);
        _damage.ChangeDamage(target, rune.Comp.Healing, targetPart: TargetBodyPart.All, canMiss: false, splitDamage: SplitDamageBehavior.None);

        if (_threshold.CheckVitalDamage(target) > deadThreshold)
            return;

        // yet another system bypassing Unrevivable etc :face_holding_back_tears:
        _mob.ChangeMobState(target, MobState.Critical, origin: user);
        if (!Mind.TryGetMind(target, out var mindId, out var mind) ||
            mind.CurrentEntity == target || // don't need a return to body prompt if you are in it already
            !Player.TryGetSessionById(mind.UserId, out var session))
            return;

        // notify them they're being revived.
        OpenReturnEui((mindId, mind), session);
    }

    protected virtual void OpenReturnEui(Entity<MindComponent> mind, ICommonSession session)
    {
    }
}
