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
        if (!_timing.IsFirstTimePredicted)
            return;

        var evSneak = new CanDoSneakAttackEvent(true);
        RaiseLocalEvent(ent, ref evSneak);
        if (!evSneak.CanSneakAttack)
            return;

        if (TryComp<MartialArtsKnowledgeComponent>(ent, out var martialArtsComp) && (martialArtsComp.Blocked || martialArtsComp.TemporaryBlockedCounter > 0))
        {
            if (Prototype(ent)?.ID is not { } entProto)
                return;
            var ev = new CanDoCQCEvent(entProto);
            RaiseLocalEvent(ent, ev);
            if (!ev.Handled)
                return;
        }

        if (!TryComp<MobStateComponent>(args.Target, out var targetState))
            return;

        if (ent.Comp.CurrentTarget is { } target && args.Target != target)
            ent.Comp.LastAttacks.Clear();

        if (TryComp<ComboActionsComponent>(ent, out var comboActions) && comboActions.QueuedPrototype is { } queued && TryComp<KnowledgeComponent>(ent, out var skillComp))
        {
            var proto = _proto.Index(queued);
            OverrideCombo(args.Performer, args.Target, proto, ent, skillComp);
            comboActions.QueuedPrototype = null;
            return;
        }

        var afterEv = new AfterComboCheckEvent(ent, args.Target, args.Weapon, args.Type);

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
        RaiseLocalEvent(ent, ref afterEv);
    }

    private void CheckCombo(Entity<CanPerformComboComponent> ent, ref ComboAttackPerformedEvent args)
    {
        var success = false;
        var target = args.Target;
        var performer = args.Performer;

        foreach (var proto in ent.Comp.AllowedCombos)
        {

            if (success)
                break;

            var sum = ent.Comp.LastAttacks.Count - proto.AttackTypes.Count;
            if (proto.AttackTypes.Count <= 0 || sum < 0)
                continue;

            var list = ent.Comp.LastAttacks.GetRange(sum, proto.AttackTypes.Count).AsEnumerable();
            var attackList = proto.AttackTypes.AsEnumerable();

            if (!TryComp<KnowledgeComponent>(ent, out var skillComponent) || skillComponent.Level < proto.LevelRequired || (skillComponent.Level > proto.LevelExceeded && proto.LevelExceeded > 0))
                continue;

            if (!list.SequenceEqual(attackList))
                continue;

            success = true;

            OverrideCombo(performer, target, proto, ent, skillComponent);
        }
    }

    public void OverrideCombo(EntityUid performer, EntityUid target, ComboPrototype proto, Entity<CanPerformComboComponent> ent, KnowledgeComponent skillComponent)
    {
        ent.Comp.Momentum += 1;

        float scale = Math.Clamp(((float) (skillComponent.Level + skillComponent.TemporaryLevel - proto.LevelRequired)) / 10.0f, 0.1f, 2.0f) + Math.Min(((float) ent.Comp.Momentum) / 20f, 2.0f);
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
            if (Prototype(ent)?.ID is not { } prototypeId)
                return;
            // TODO: limit it to be based on your opponent's martial art level + 10
            var ev = new AddExperienceEvent(prototypeId, 1, 10);
            RaiseLocalEvent(performer, ref ev);
        }

        Dirty(ent);
    }
}
