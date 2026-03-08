// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// This handles determining if a combo was performed.
/// </summary>
public partial class MartialArtsSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private EntityQuery<CanPerformComboComponent> _comboQuery;

    private void InitializeCanPerformCombo()
    {
        _comboQuery = GetEntityQuery<CanPerformComboComponent>();

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

        if (TryComp<ComboActionsComponent>(ent, out var comboActions) && comboActions.QueuedPrototype is { } queued)
        {
            var proto = _proto.Index(queued);
            var level = _knowledge.GetLevel(ent.Owner);
            OverrideCombo(user, args.Target, proto, ent, level);
            comboActions.QueuedPrototype = null;
            return;
        }

        ent.Comp.CurrentTarget = args.Target;
        ent.Comp.ResetTime = _timing.CurTime + TimeSpan.FromSeconds(5);
        ent.Comp.LastAttacks.Add(args.Type);
        if (ent.Comp.LastAttacksLimit >= 0)
        {
            var difference = ent.Comp.LastAttacks.Count - ent.Comp.LastAttacksLimit;
            if (difference > 0)
                ent.Comp.LastAttacks.RemoveRange(0, difference);
        }
        CheckCombo(ent, ref args);
    }

    private void CheckCombo(Entity<CanPerformComboComponent> ent, ref ComboAttackPerformedEvent args)
    {
        var target = args.Target;
        var performer = args.Performer;
        var level = _knowledge.GetLevel(ent.Owner);

        foreach (var proto in ent.Comp.AllowedCombos)
        {
            var sum = ent.Comp.LastAttacks.Count - proto.AttackTypes.Count;
            if (proto.AttackTypes.Count <= 0 || sum < 0)
                continue;

            var list = ent.Comp.LastAttacks.GetRange(sum, proto.AttackTypes.Count).AsEnumerable();
            var attackList = proto.AttackTypes.AsEnumerable();

            if (level < proto.LevelRequired || (level > proto.LevelExceeded && proto.LevelExceeded > 0))
                continue;

            if (!list.SequenceEqual(attackList))
                continue;

            OverrideCombo(performer, target, proto, ent, level);
            break; // found the combo
        }
    }

    public void OverrideCombo(EntityUid performer, EntityUid target, ComboPrototype proto, Entity<CanPerformComboComponent> ent, int level)
    {
        ent.Comp.Momentum += 1;

        float scale = Math.Clamp(((float) (level - proto.LevelRequired)) / 10.0f, 0.1f, 2.0f) + Math.Min(((float) ent.Comp.Momentum) / 20f, 2.0f);
        var evDamage = new MartialArtDamageModifierEvent(performer, 1);
        RaiseLocalEvent(ent, ref evDamage);
        scale *= evDamage.Coefficient;

        if (proto.UserEffects != null)
            _effects.ApplyEffects(performer, proto.UserEffects, scale, target);
        if (proto.OpponentEffects != null)
            _effects.ApplyEffects(target, proto.OpponentEffects, scale, performer);

        ent.Comp.LastAttacks.Clear();

        if (TryComp<MartialArtsKnowledgeComponent>(ent, out var martialArtsComp) && !martialArtsComp.Blocked && _mobState.IsAlive(target) && proto.GiveExperience)
        {
            if (Prototype(ent)?.ID is not { } prototypeId || _knowledge.GetContainer(performer) is not { } brain)
                return;
            // TODO: limit it to be based on your opponent's martial art level + 10
            _knowledge.AddExperience(brain, prototypeId, 1, 10);
        }

        Dirty(ent);
    }
}
