using Content.Shared.Access.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Hypospray.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.Medical.Hypoport;

/// <summary>
/// Prevents hypospray injections without a hypoport or if you aren't grabbing the patient.
/// </summary>
public sealed class HypoportSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<HypoportComponent> _query;
    private EntityQuery<IgnoreHypoportComponent> _ignoreQuery;
    private EntityQuery<PullerComponent> _pullerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<HypoportComponent>();
        _ignoreQuery = GetEntityQuery<IgnoreHypoportComponent>();
        _pullerQuery = GetEntityQuery<PullerComponent>();

        SubscribeLocalEvent<BodyComponent, TargetBeforeHyposprayInjectsEvent>(OnBeforeHyposprayInjects);

        SubscribeLocalEvent<HypoportAccessComponent, HypoportInjectAttemptEvent>(OnAccessInjectAttempt);
    }

    private void OnBeforeHyposprayInjects(Entity<BodyComponent> ent, ref TargetBeforeHyposprayInjectsEvent args)
    {
        if (args.Cancelled)
            return;

        var used = args.Hypospray;
        if (_ignoreQuery.HasComp(used))
            return;

        // holy verbose names batman
        var target = args.TargetGettingInjected;
        var user = args.EntityUsingHypospray;

        // first require that the user is being (at least) softgrabbed, so surprise injections are cooler (grabbed then prick prick prick)
        // it makes sense since youd need to get a hold of someone to properly connect to their neck's port
        // of course ignore this if you are injecting yourself
        if (user != target && _pullerQuery.TryComp(user, out var puller) && puller.Pulling != target)
        {
            args.InjectMessageOverride = "hypoport-fail-grab";
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
        args.InjectMessageOverride = message ?? "hypoport-fail-missing";
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
}
