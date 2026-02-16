// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.HealthExaminable;
using Content.Shared.IdentityManagement;

namespace Content.Trauma.Shared.Body.Organ;

public sealed class OrganExamineSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, HealthBeingExaminedEvent>(_body.RelayBodyEvent);
        SubscribeLocalEvent<OrganExamineComponent, BodyRelayedEvent<HealthBeingExaminedEvent>>(OnHealthExamined);
    }

    private void OnHealthExamined(Entity<OrganExamineComponent> ent, ref BodyRelayedEvent<HealthBeingExaminedEvent> args)
    {
        var identity = Identity.Entity(ent.Owner, EntityManager);
        var message = args.Args.Message;
        message.AddMarkupOrThrow(Loc.GetString(ent.Comp.Examine, ("target", identity), ("organ", ent)));
        message.PushNewline();
    }
}
