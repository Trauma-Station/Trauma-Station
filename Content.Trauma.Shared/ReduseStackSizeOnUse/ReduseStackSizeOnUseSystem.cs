// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction.Events;
using Content.Shared.Stacks;

namespace Content.Trauma.Shared.ReduseStackSizeOnUse;

public sealed partial class ReduseStackSizeOnUseSystem : EntitySystem
{
    [Dependency] private SharedStackSystem _stack = default!;

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<ReduseStackSizeOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (TryComp<StackComponent>(ent, out var stack))
        {
            _stack.ReduceCount((ent.Owner, stack), 1);
            return;
        }

        // It's consumed on use and it's not a stack so delete it
        PredictedQueueDel(ent);
    }
}
