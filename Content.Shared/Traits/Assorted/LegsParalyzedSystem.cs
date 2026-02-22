// <Trauma>
using Content.Shared.Stunnable;
// </Trauma>
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;

namespace Content.Shared.Traits.Assorted;

public sealed class LegsParalyzedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LegsParalyzedComponent, BuckledEvent>(OnBuckled);
        SubscribeLocalEvent<LegsParalyzedComponent, UnbuckledEvent>(OnUnbuckled);
        SubscribeLocalEvent<LegsParalyzedComponent, ThrowPushbackAttemptEvent>(OnThrowPushbackAttempt);
        // <Trauma>
        // SubscribeLocalEvent<LegsParalyzedComponent, UpdateCanMoveEvent>(OnUpdateCanMoveEvent);
        SubscribeLocalEvent<LegsParalyzedComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<LegsParalyzedComponent, StandUpAttemptEvent>(OnStandAttempt);
        // </Trauma>
    }

    // <Trauma>
    private void OnStandAttempt(Entity<LegsParalyzedComponent> ent, ref StandUpAttemptEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancelled = true;
    }

    private void OnRefresh(Entity<LegsParalyzedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.WalkSpeedModifier, ent.Comp.SprintSpeedModifier, true);
    }
    // </Trauma>

    private void OnStartup(EntityUid uid, LegsParalyzedComponent component, ComponentStartup args)
    {
        // TODO: In future probably must be surgery related wound
        EnsureComp<KnockedDownComponent>(uid); // Trauma
    }

    private void OnShutdown(EntityUid uid, LegsParalyzedComponent component, ComponentShutdown args)
    {
        // <Trauma>
        // _standingSystem.Stand(uid);
        RemCompDeferred<KnockedDownComponent>(uid);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
        // </Trauma>
    }

    private void OnBuckled(EntityUid uid, LegsParalyzedComponent component, ref BuckledEvent args)
    {
        _standingSystem.Stand(uid, force: true); // Trauma;
    }

    private void OnUnbuckled(EntityUid uid, LegsParalyzedComponent component, ref UnbuckledEvent args)
    {
        _standingSystem.Down(uid);
    }

    /*private void OnUpdateCanMoveEvent(EntityUid uid, LegsParalyzedComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }*/

    private void OnThrowPushbackAttempt(EntityUid uid, LegsParalyzedComponent component, ThrowPushbackAttemptEvent args)
    {
        args.Cancel();
    }
}
