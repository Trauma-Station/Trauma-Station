using Content.Shared._Shitcode.Heretic.Curses;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
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

        SubscribeLocalEvent<FragileCurseComponent, DamageModifyEvent>(OnModify);
    }

    private void OnModify(Entity<FragileCurseComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.ModifierSet);
    }

    private void OnParalysisApply(Entity<CurseOfParalysisStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (TryComp(args.Target, out LegsParalyzedComponent? paralyzed) && paralyzed.Permanent)
        {
            ent.Comp.WasParalyzed = true;
            return;
        }

        var comp = Factory.GetComponent<LegsParalyzedComponent>();
        comp.Permanent = false;
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
