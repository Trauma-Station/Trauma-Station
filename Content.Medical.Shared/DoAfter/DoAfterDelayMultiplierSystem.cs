// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.DoAfter;
using Content.Shared.Body;

namespace Content.Medical.Shared.DoAfter;

public sealed class DoAfterDelayMultiplierSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ModifyDoAfterDelayEvent>(_body.RelayBodyEvent);
        SubscribeLocalEvent<DoAfterDelayMultiplierComponent, BodyRelayedEvent<ModifyDoAfterDelayEvent>>(OnModify);
    }

    private void OnModify(Entity<DoAfterDelayMultiplierComponent> ent, ref BodyRelayedEvent<ModifyDoAfterDelayEvent> args)
    {
        args.Args.Multiplier *= ent.Comp.Multiplier;
    }
}
