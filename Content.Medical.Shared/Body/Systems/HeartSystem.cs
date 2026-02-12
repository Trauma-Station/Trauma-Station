// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Shared.DelayedDeath;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

// TODO SHITMED: make this generic vital organs system instead of copy pasting with brain system bruh
public sealed class HeartSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;

    public static readonly ProtoId<OrganCategoryPrototype> Brain = "Brain";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeartComponent, OrganGotInsertedEvent>(HandleAddition);
        SubscribeLocalEvent<HeartComponent, OrganGotRemovedEvent>(HandleRemoval);
    }

    private void HandleRemoval(EntityUid uid, HeartComponent _, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        // TODO: Add some form of very violent bleeding effect.
        EnsureComp<DelayedDeathComponent>(args.Target);
    }

    private void HandleAddition(EntityUid uid, HeartComponent _, ref OrganGotInsertedEvent args)
    {
        if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(args.Target))
            return;

        if (_body.GetOrgan(args.Target, Brain) != null)
            RemComp<DelayedDeathComponent>(args.Target);
    }
}
