// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Gibbing;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Medical.Shared.Body;

public sealed class UnremoveableOrganSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnremoveableOrganComponent, OrganRemoveAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<UnremoveableOrganComponent, OrganGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<UnremoveableOrganComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    private void OnRemoveAttempt(Entity<UnremoveableOrganComponent> ent, ref OrganRemoveAttemptEvent args)
    {
        args.Cancelled |= ent.Owner == args.Organ;
    }

    private void OnRemoved(Entity<UnremoveableOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || Transform(args.Target).MapID == MapId.Nullspace || _timing.ApplyingState)
            return; // all good if it's being deleted or leaving pvs range

        // if you intentionally deleted the root part, please delete the body instead chud
        if (!TerminatingOrDeleted(ent) && !HasComp<ChildOrganComponent>(ent))
        {
            Log.Warning($"{ToPrettyString(ent)} was deleted instead of the body, {ToPrettyString(args.Target)}!");
            PredictedQueueDel(args.Target);
        }

        Log.Warning($"{ToPrettyString(ent)} somehow got removed from {ToPrettyString(args.Target)}!");
    }

    private void OnBeingGibbed(Entity<UnremoveableOrganComponent> ent, ref BeingGibbedEvent args)
    {
        // this is specifically if the torso gets gibbed, if the body is gibbed it uses BodyRelayedEvent anyway
        if (HasComp<ChildOrganComponent>(ent) || _body.GetBody(ent.Owner) is not {} body)
            return;

        Log.Info($"Root part {ToPrettyString(ent)} was gibbed, gibbing {ToPrettyString(ent)} too!");
        _gibbing.Gib(body);
    }
}
