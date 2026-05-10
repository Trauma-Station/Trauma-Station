// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.EntityConditions;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// This handles determining if a combo was performed.
/// </summary>
public partial class MartialArtsSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private readonly EntityQuery<CanPerformComboComponent> _comboQuery = default!;

    private void InitializeCanPerformCombo()
    {
        SubscribeLocalEvent<CanPerformComboComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CanPerformComboComponent, ComboAttackPerformedEvent>(OnComboAttackPerformed);
    }

    private void OnMapInit(Entity<CanPerformComboComponent> ent, ref MapInitEvent args)
    {
        foreach (var item in ent.Comp.RoundstartCombos)
        {
            ent.Comp.AllowedCombos.Add(_proto.Index(item));
        }
        Dirty(ent);
    }

    private void OnComboAttackPerformed(Entity<CanPerformComboComponent> ent, ref ComboAttackPerformedEvent args)
    {
        var user = args.Performer;
        // ignore attacks that use weapons...
        if (!_timing.IsFirstTimePredicted || args.Weapon != user)
            return;

        var attemptEv = new ComboAttemptEvent();
        RaiseLocalEvent(ent, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        if (TryComp<MartialArtsKnowledgeComponent>(ent, out var martialArtsComp) && martialArtsComp.Blocked)
            return;

        if (!TryComp<MobStateComponent>(args.Target, out var targetState))
            return;

        if (ent.Comp.CurrentTarget is { } target && args.Target != target)
            ent.Comp.LastAttacks.Clear();

        ent.Comp.CurrentTarget = args.Target;
        ent.Comp.ResetTime = _timing.CurTime + TimeSpan.FromSeconds(5);
        ent.Comp.LastAttacks.Add(args.Type);
        if (ent.Comp.LastAttacksLimit >= 0)
        {
            var difference = ent.Comp.LastAttacks.Count - ent.Comp.LastAttacksLimit;
            if (difference > 0)
                ent.Comp.LastAttacks.RemoveRange(0, difference);
        }

        if (TryComp<ComboActionsComponent>(ent, out var comboActions) && comboActions.QueuedPrototype is { } queued)
        {
            var proto = _proto.Index(queued);
            var level = _knowledge.GetLevel(ent.Owner);

            if (!CheckCombo(ent, proto, level, user, args.Target))
                return;

            PerformCombo(user, args.Target, proto, ent, level);
            comboActions.QueuedPrototype = null;
            return;
        }

        TryPerformCombo(ent, ref args);
    }

    private void TryPerformCombo(Entity<CanPerformComboComponent> ent, ref ComboAttackPerformedEvent args)
    {
        var target = args.Target;
        var performer = args.Performer;
        var level = _knowledge.GetLevel(ent.Owner);

        foreach (var proto in ent.Comp.AllowedCombos)
        {
            if (!CheckCombo(ent, proto, level, performer, target))
                continue;

            PerformCombo(performer, target, proto, ent, level);
            break; // found the combo
        }
    }

    private bool CheckCombo(Entity<CanPerformComboComponent> ent,
        ComboPrototype proto,
        int level,
        EntityUid performer,
        EntityUid target)
    {
        var sum = ent.Comp.LastAttacks.Count - proto.AttackTypes.Count;
        if (proto.AttackTypes.Count <= 0 || sum < 0)
            return false;

        var list = ent.Comp.LastAttacks.GetRange(sum, proto.AttackTypes.Count).AsEnumerable();
        var attackList = proto.AttackTypes.AsEnumerable();

        if (level < proto.LevelRequired || (level > proto.LevelExceeded && proto.LevelExceeded > 0) ||
            !list.SequenceEqual(attackList) ||
            !_conditions.TryConditions(performer, proto.UserConditions) ||
            !_conditions.TryConditions(target, proto.Conditions))
            return false;

        return true;
    }

    public void PerformCombo(EntityUid performer, EntityUid target, ComboPrototype proto, Entity<CanPerformComboComponent> ent, int level)
    {
        // TODO: dont hardcode this here...
        ent.Comp.Momentum += 1;

        var scaleEv = new MartialArtModifyScaleEvent(performer);
        RaiseLocalEvent(ent, ref scaleEv);
        var scale = scaleEv.Scale;

        if (proto.UserEffects != null)
            _effects.ApplyEffects(performer, proto.UserEffects, scale, user: performer);
        if (proto.OpponentEffects != null)
            _effects.ApplyEffects(target, proto.OpponentEffects, scale, user: performer);

        ent.Comp.LastAttacks.Clear();
        Dirty(ent);

        var ev = new ComboPerformedEvent(performer, target);
        RaiseLocalEvent(ent, ref ev);
        // TODO: move xp logic into an event handler
        if (TryComp<MartialArtsKnowledgeComponent>(ent, out var comp) && comp.GiveExperience && !comp.Blocked && _mobState.IsAlive(target) && proto.GiveExperience)
        {
            // you can only go up to your opponents level + 10, to encourage actual training between masters
            var opponent = GetMartialArtLevel(target);
            _knowledge.AddExperience(ent.Owner, performer, 1, opponent + 10);
        }
    }

    private int GetMartialArtLevel(EntityUid uid)
        => _knowledge.GetActiveMartialArt(uid) is { } unit
            ? _knowledge.GetLevel(unit)
            : 0;
}
