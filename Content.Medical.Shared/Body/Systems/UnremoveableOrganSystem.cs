// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Medical.Shared.Body;

public sealed class UnremoveableOrganSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnremoveableOrganComponent, OrganRemoveAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<UnremoveableOrganComponent, OrganGotRemovedEvent>(OnRemoved);
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
        if (!TerminatingOrDeleted(ent))
        {
            Log.Warning($"{ToPrettyString(ent)} was deleted instead of the body, {ToPrettyString(args.Target)}!");
            PredictedQueueDel(args.Target);
        }

        Log.Warning($"{ToPrettyString(ent)} somehow got removed from {ToPrettyString(args.Target)}!");
    }
}
