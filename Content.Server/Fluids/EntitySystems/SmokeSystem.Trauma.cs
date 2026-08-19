using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class SmokeSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mob = default!;

    private void TouchReact(EntityUid entity, Solution solution, SmokeComponent component)
    {
        var cloneSolution = solution.Clone();
        var transferAmount = FixedPoint2.Min(cloneSolution.Volume, component.TransferRate);
        var transferSolution = cloneSolution.SplitSolution(transferAmount);

        foreach (var reagentQuantity in transferSolution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;

            _reactive.ReactionEntity(entity, ReactionMethod.Touch, reagentQuantity);
        }
    }
}
