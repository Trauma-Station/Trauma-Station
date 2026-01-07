// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Access.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Medical.Hypoport;

/// <summary>
/// Prevents hypospray injections without a hypoport or if you aren't grabbing the patient.
/// </summary>
public sealed class HypoportSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    private EntityQuery<HypoportComponent> _query;
    private EntityQuery<IgnoreHypoportComponent> _ignoreQuery;
    private EntityQuery<InjectorComponent> _injectorQuery;
    private EntityQuery<PullerComponent> _pullerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<HypoportComponent>();
        _ignoreQuery = GetEntityQuery<IgnoreHypoportComponent>();
        _injectorQuery = GetEntityQuery<InjectorComponent>();
        _pullerQuery = GetEntityQuery<PullerComponent>();

        SubscribeLocalEvent<BodyComponent, TargetBeforeInjectEvent>(OnBeforeInject);

        SubscribeLocalEvent<HypoportAccessComponent, HypoportInjectAttemptEvent>(OnAccessInjectAttempt);
    }

    private void OnBeforeInject(Entity<BodyComponent> ent, ref TargetBeforeInjectEvent args)
    {
        if (args.Cancelled)
            return;

        var used = args.UsedInjector;
        if (_ignoreQuery.HasComp(used) || !IsHypospray(used))
            return;

        // holy verbose names batman
        var target = args.TargetGettingInjected;
        var user = args.EntityUsingInjector;
        var targetIdent = Identity.Entity(target, EntityManager);

        // first require that the user is being (at least) softgrabbed, so surprise injections are cooler (grabbed then prick prick prick)
        // it makes sense since youd need to get a hold of someone to properly connect to their neck's port
        // of course ignore this if you are injecting yourself
        if (user != target && _pullerQuery.TryComp(user, out var puller) && puller.Pulling != target)
        {
            args.OverrideMessage = Loc.GetString("hypoport-fail-grab", ("target", targetIdent));
            args.Cancel();
            return;
        }

        // now find a hypoport that allows injection
        LocId? message = null;
        foreach (var (id, _) in _body.GetBodyOrgans(ent, ent.Comp))
        {
            if (!_query.HasComp(id))
                continue;

            // check if this hypoport is allowed to be used
            var ev = new HypoportInjectAttemptEvent(target, user, used);
            RaiseLocalEvent(id, ref ev);
            if (!ev.Cancelled)
                return; // this port is valid, let the event go through

            // use the first failing hypoport's message incase there are multiple (evil)
            message ??= ev.InjectMessageOverride;
        }

        // no valid port found. say there were none unless an existing port prevented injection
        args.OverrideMessage = Loc.GetString(message ?? "hypoport-fail-missing", ("target", targetIdent));
        args.Cancel();
    }

    private void OnAccessInjectAttempt(Entity<HypoportAccessComponent> ent, ref HypoportInjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_accessReader.IsAllowed(args.User, ent.Owner))
        {
            args.InjectMessageOverride = "hypoport-fail-access";
            args.Cancelled = true;
        }
    }

    private bool IsHypospray(EntityUid uid)
    {
        var comp = _injectorQuery.Comp(uid);
        if (!_proto.Resolve(comp.ActiveModeProtoId, out var mode))
            return false; // invalid injector but not my problem

        // instant injection into mobs means hypospray
        return mode.DelayPerVolume == TimeSpan.Zero && mode.MobTime == TimeSpan.Zero;
    }
}
