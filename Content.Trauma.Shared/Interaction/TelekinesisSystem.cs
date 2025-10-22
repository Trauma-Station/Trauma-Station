using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.Cuffs;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Stunnable;

namespace Content.Trauma.Shared.Interaction;

public sealed class TelekinesisSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<AdminFrozenComponent> _frozenQuery;

    public override void Initialize()
    {
        base.Initialize();

        _frozenQuery = GetEntityQuery<AdminFrozenComponent>();

        // this is evil but preferable to making a new event to uncancel interaction attempts.
        // anything important that might accidentally get overriden (admin freeze) is already checked in CanUseTelekinesis
        SubscribeLocalEvent<TelekinesisComponent, InteractionAttemptEvent>(OnInteractionAttempt,
            after: new[] { typeof(SharedStunSystem), typeof(SharedCuffableSystem) });
        SubscribeLocalEvent<TelekinesisComponent, InRangeOverrideEvent>(OnRangeOverride);
    }

    private void OnInteractionAttempt(Entity<TelekinesisComponent> ent, ref InteractionAttemptEvent args)
    {
        // overwrite previous cancel from stunned, cuffed etc
        args.Cancelled = !CanUseTelekinesis(ent);
    }

    private void OnRangeOverride(Entity<TelekinesisComponent> ent, ref InRangeOverrideEvent args)
    {
        args.Handled = true;
        args.InRange = IsInRange(args.User, args.Target, args.Range);
    }

    public bool CanUseTelekinesis(EntityUid uid)
    {
        // never let players bypass admin freeze
        if (_frozenQuery.HasComp(uid))
            return false;

        // can't use telekinesis if you are eepy
        return _blocker.CanConsciouslyPerformAction(uid);
    }

    public bool IsInRange(EntityUid user, EntityUid target, float range)
    {
        var xform = Transform(user);
        var targetXform = Transform(target);
        if (xform.MapUid != targetXform.MapUid)
            return false; // telekinetic not fucking god

        var pos = _transform.GetMapCoordinates(user, xform).Position;
        var targetPos = _transform.GetMapCoordinates(target, targetXform).Position;
        var dist2 = (pos - targetPos).LengthSquared();
        var r2 = range * range;
        return dist2 <= r2;
    }
}
