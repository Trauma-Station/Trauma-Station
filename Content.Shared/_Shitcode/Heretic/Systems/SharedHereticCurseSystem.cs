using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Curses;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Timing;

namespace Content.Shared._Shitcode.Heretic.Systems;

public abstract class SharedHereticCurseSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurseOfParalysisStatusEffectComponent, StatusEffectAppliedEvent>(OnParalysisApply);
        SubscribeLocalEvent<CurseOfParalysisStatusEffectComponent, StatusEffectRemovedEvent>(OnParalysisRemove);
        SubscribeLocalEvent<CurseOfAmokStatusEffectComponent, StatusEffectAppliedEvent>(OnAmokApply);
        SubscribeLocalEvent<CurseOfAmokStatusEffectComponent, StatusEffectRemovedEvent>(OnAmokRemove);
        SubscribeLocalEvent<CurseOfFragilityStatusEffectComponent, StatusEffectAppliedEvent>(OnFragilityApply);
        SubscribeLocalEvent<CurseOfFragilityStatusEffectComponent, StatusEffectRemovedEvent>(OnFragilityRemove);
        SubscribeLocalEvent<CurseOfBlindnessStatusEffectComponent, StatusEffectAppliedEvent>(OnBlindnessApply);
        SubscribeLocalEvent<CurseOfBlindnessStatusEffectComponent, StatusEffectRemovedEvent>(OnBlindnessRemove);

        SubscribeLocalEvent<FragileCurseComponent, DamageModifyEvent>(OnModify);
    }

    private void OnModify(Entity<FragileCurseComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.ModifierSet);
    }

    private void OnBlindnessApply(Entity<CurseOfBlindnessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<TemporaryBlindnessComponent>(args.Target);
    }

    private void OnBlindnessRemove(Entity<CurseOfBlindnessStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (Timing.ApplyingState)
            return;

        if (TerminatingOrDeleted(args.Target))
            return;

        RemCompDeferred<TemporaryBlindnessComponent>(args.Target);
    }

    private void OnFragilityApply(Entity<CurseOfFragilityStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<FragileCurseComponent>(args.Target);
    }

    private void OnFragilityRemove(Entity<CurseOfFragilityStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (Timing.ApplyingState)
            return;

        if (TerminatingOrDeleted(args.Target))
            return;

        RemCompDeferred<FragileCurseComponent>(args.Target);
    }

    private void OnAmokApply(Entity<CurseOfAmokStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        var affected = EnsureComp<EntropicPlumeAffectedComponent>(args.Target);
        affected.Duration = float.MaxValue;
        affected.Sprite = null;
        Dirty(args.Target, affected);
    }

    private void OnAmokRemove(Entity<CurseOfAmokStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (Timing.ApplyingState)
            return;

        if (TerminatingOrDeleted(args.Target))
            return;

        RemCompDeferred<EntropicPlumeAffectedComponent>(args.Target);
    }

    private void OnParalysisApply(Entity<CurseOfParalysisStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (HasComp<LegsParalyzedComponent>(args.Target))
        {
            ent.Comp.WasParalyzed = true;
            return;
        }

        var comp = Factory.GetComponent<LegsParalyzedComponent>();
        comp.WalkSpeedModifier = 0.5f;
        comp.SprintSpeedModifier = 0.5f;
        AddComp(args.Target, comp, true);
    }

    private void OnParalysisRemove(Entity<CurseOfParalysisStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (Timing.ApplyingState || ent.Comp.WasParalyzed)
            return;

        if (TerminatingOrDeleted(args.Target))
            return;

        RemCompDeferred<LegsParalyzedComponent>(args.Target);
    }
}
