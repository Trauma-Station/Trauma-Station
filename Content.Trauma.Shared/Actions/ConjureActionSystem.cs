// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Hands.EntitySystems;

namespace Content.Trauma.Shared.Actions;

public sealed class ConjureActionSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConjureActionComponent, ConjureActionEvent>(OnAction);
    }

    private void OnAction(Entity<ConjureActionComponent> ent, ref ConjureActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.Performer;
        var spawned = PredictedSpawnAtPosition(ent.Comp.Spawn, Transform(user).Coordinates);
        _hands.TryPickupAnyHand(user, spawned, animate: false);
        // TODO admin log
    }
}
