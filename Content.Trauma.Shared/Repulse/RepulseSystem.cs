// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Trauma.Shared.Repulse;

public sealed partial class RepulseSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<RepulseComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RepulseOnCollideComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<RepulseComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnStartCollide(Entity<RepulseOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (!_query.TryComp(ent, out var repulse))
            return;

        TryRepulse((ent.Owner, repulse), args.OtherEntity);
    }

    private void OnInteractHand(Entity<RepulseComponent> ent, ref InteractHandEvent args)
    {
        TryRepulse(ent, args.User);
    }

    public void TryRepulse(Entity<RepulseComponent> ent, EntityUid target)
    {
        var ev = new RepulseAttemptEvent(target);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
            return;

        var direction = _transform.GetMapCoordinates(target).Position - _transform.GetMapCoordinates(ent).Position;
        var impulse = direction.Normalized() * ent.Comp.Impulse;

        _physics.ApplyLinearImpulse(target, impulse);
        _stun.TryUpdateParalyzeDuration(target, ent.Comp.StunDuration);
        _stun.TryKnockdown(target, ent.Comp.KnockdownDuration, true, drop: true);
    }
}
