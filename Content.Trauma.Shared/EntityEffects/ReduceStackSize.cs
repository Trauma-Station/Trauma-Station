// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Materials;
using Content.Shared.Stacks;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Reduses stack count by set amount, if there just one item in stuck left - deletes it
/// </summary>
public sealed partial class ReduceStackSize : EntityEffectBase<ReduceStackSize>
{
    [DataField]
    public int Amount = 1;
}

public sealed partial class ReduceStackSizeSystem : EntityEffectSystem<PhysicalCompositionComponent, ReduceStackSize>
{
    [Dependency] private SharedStackSystem _stack = default!;

    protected override void Effect(Entity<PhysicalCompositionComponent> ent, ref EntityEffectEvent<ReduceStackSize> args)
    {
        if (TryComp<StackComponent>(ent, out var stack))
        {
            _stack.ReduceCount((ent.Owner, stack), args.Effect.Amount);
            return;
        }

        // It's consumed and it's not a stack so delete it
        PredictedQueueDel(ent);
    }
}
