using System.Linq;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;

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
        SubscribeLocalEvent<CanPerformComboComponent, ComboBeingPerformedEvent>(OnComboBeingPerformed);
        SubscribeLocalEvent<CanPerformComboComponent, SaveLastAttacksEvent>(OnSave);
        SubscribeLocalEvent<CanPerformComboComponent, ResetLastAttacksEvent>(OnReset);
        SubscribeLocalEvent<CanPerformComboComponent, LoadLastAttacksEvent>(OnLoad);
    }

    private void OnLoad(Entity<CanPerformComboComponent> ent, ref LoadLastAttacksEvent args)
    {
        if (ent.Comp.LastAttacksSaved == null)
            return;

        ent.Comp.LastAttacks = ent.Comp.LastAttacksSaved;
        ent.Comp.LastAttacksSaved = null;

        if (args.Dirty)
            Dirty(ent);
    }

    private void OnReset(Entity<CanPerformComboComponent> ent, ref ResetLastAttacksEvent args)
    {
        ent.Comp.LastAttacks.Clear();

        if (args.Dirty)
            Dirty(ent);
    }

    private void OnSave(Entity<CanPerformComboComponent> ent, ref SaveLastAttacksEvent args)
    {
        ent.Comp.LastAttacksSaved = new(ent.Comp.LastAttacks);
    }

    private void OnMapInit(Entity<CanPerformComboComponent> ent, ref MapInitEvent args)
    {
        foreach (var item in ent.Comp.RoundstartCombos)
        {
            ent.Comp.AllowedCombos.Add(_proto.Index(item));
        }
    }

    private void OnComboAttackPerformed(Entity<CanPerformComboComponent> ent, ref ComboAttackPerformedEvent args)
    {
        if (TryComp<SneakAttackComponent>(ent, out var sneakAttack) && sneakAttack.IsFound)
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
        CheckCombo(ent, args.Target, ent.Comp, ref args);
        if (targetState.CurrentState == MobState.Alive && args.Type != ComboAttackType.Hug)
        {
            var prototypeId = Prototype(ent.Owner)?.ID;
            if (prototypeId != null)
            {
                var ev = new AddExperienceEvent(prototypeId, 1);
                RaiseLocalEvent(args.Performer, ref ev);
            }
        }
        RaiseLocalEvent(ent, ref afterEv);
        Dirty(ent, ent.Comp);
    }

    private void CheckCombo(EntityUid uid, EntityUid target, CanPerformComboComponent comp, ref ComboAttackPerformedEvent args)
    {
        var success = false;

        foreach (var proto in comp.AllowedCombos)
        {

            if (success)
                break;

            var sum = comp.LastAttacks.Count - proto.AttackTypes.Count;
            if (sum < 0)
                continue;

            var list = comp.LastAttacks.GetRange(sum, proto.AttackTypes.Count).AsEnumerable();
            var attackList = proto.AttackTypes.AsEnumerable();

            if (!list.SequenceEqual(attackList))
                continue;

            if (!TryComp<KnowledgeComponent>(uid, out var skillComponent) || skillComponent.Level < proto.LevelRequired || (skillComponent.Level > proto.LevelExceeded && proto.LevelExceeded > 0))
                continue;


            var beingPerformedEv = new ComboBeingPerformedEvent(proto.ID);
            RaiseLocalEvent(uid, ref beingPerformedEv);
            comp.Momentum += 1;

            float scale = Math.Clamp(((float) (skillComponent.Level + skillComponent.TemporaryLevel - proto.LevelRequired)) / 10.0f, 0.1f, 2.0f) + Math.Min(((float) comp.Momentum) / 20f, 2.0f);

            if (proto.UserEffects != null)
                _effects.ApplyEffects(args.Performer, proto.UserEffects, scale, args.Target);
            if (proto.OpponentEffects != null)
                _effects.ApplyEffects(args.Target, proto.OpponentEffects, scale, args.Performer);

            comp.LastAttacks.Clear();
            success = true;
            if (TryComp<MartialArtsKnowledgeComponent>(uid, out var martialArtsComp) && !martialArtsComp.Blocked && (!_mobState.IsDead(args.Target) && _mobState.IsCritical(args.Target)))
            {
                var prototypeId = Prototype(uid)?.ID;
                if (prototypeId is {})
                {
                    var ev = new AddExperienceEvent(prototypeId, 1);
                    RaiseLocalEvent(args.Performer, ref ev);
                }
            }
        }
    }
    private void OnComboBeingPerformed(Entity<CanPerformComboComponent> ent, ref ComboBeingPerformedEvent args)
    {
        ent.Comp.BeingPerformed = args.Combo;
    }
}
