using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Examine;
using Content.Shared.Heretic;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Shitcode.Heretic.Systems;

public abstract class SharedForestAdmonitionsSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedTransformSystem XForm = default!;

    [Dependency] private readonly SharedShadowCloakSystem _cloak = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> IgnoreTag = "SpellIgnoreForestAdmonitions";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticActionComponent, ActionPerformedEvent>(OnAction);

        SubscribeLocalEvent<ForestAdmonitionsComponent, SelfBeforeGunShotEvent>(OnShot);
        SubscribeLocalEvent<ForestAdmonitionsComponent, BeforeThrowEvent>(OnThrow);
        SubscribeLocalEvent<ForestAdmonitionsComponent, UserInvokeTouchSpellEvent>(OnTouchSpellInvoke);

        SubscribeLocalEvent<ForestAdmonitionsEntityComponent, ExamineAttemptEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<ForestAdmonitionsEntityComponent> ent, ref ExamineAttemptEvent args)
    {
        if (CalculateVisibilityFactor(ent, args.Examiner) < ent.Comp.ExamineThreshold)
            args.Cancel();
    }

    private void OnTouchSpellInvoke(Entity<ForestAdmonitionsComponent> ent, ref UserInvokeTouchSpellEvent args)
    {
        RevealCloak(ent.AsNullable());
    }

    private void OnThrow(Entity<ForestAdmonitionsComponent> ent, ref BeforeThrowEvent args)
    {
        RevealCloak(ent.AsNullable());
    }

    private void OnShot(Entity<ForestAdmonitionsComponent> ent, ref SelfBeforeGunShotEvent args)
    {
        RevealCloak(ent.AsNullable());
    }

    private void OnAction(Entity<HereticActionComponent> ent, ref ActionPerformedEvent args)
    {
        if (_tag.HasTag(ent, IgnoreTag))
            return;

        RevealCloak(args.Performer);
    }

    private void RevealCloak(Entity<ForestAdmonitionsComponent?, ShadowCloakedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        if (_cloak.GetShadowCloakEntity(ent) is not { } cloak ||
            !TryComp(cloak, out ForestAdmonitionsEntityComponent? comp))
            return;

        comp.LastRevealTime = Timing.CurTime;
        comp.UpdateAccumulator = 0f;
        Dirty(cloak, comp);
    }

    protected float CalculateVisibilityFactor(Entity<ForestAdmonitionsEntityComponent> ent, EntityUid viewer)
    {
        var diff = (float) (Timing.CurTime.TotalSeconds - ent.Comp.LastRevealTime.TotalSeconds);
        var factor = Math.Clamp(1f - diff / ent.Comp.RevealDuration, 0f, 1f);
        if (ent.Owner == viewer)
            return factor == 0f ? ent.Comp.SelfVisibility : 1f;

        var us = XForm.GetMapCoordinates(ent);
        var them = XForm.GetMapCoordinates(viewer);

        if (us.MapId != them.MapId)
            return 0f;

        var distance = (us.Position - them.Position).Length();
        factor += Math.Clamp(1f - distance / ent.Comp.RevealDistance, 0f, 1f);
        return Math.Clamp(factor, 0f, 1f);
    }
}
