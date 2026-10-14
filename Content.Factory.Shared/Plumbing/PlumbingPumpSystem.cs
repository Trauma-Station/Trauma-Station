// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Machines;
using Content.Factory.Shared.Slots;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Factory.Shared.Plumbing;

public sealed partial class PlumbingPumpSystem : EntitySystem
{
    [Dependency] private ExclusiveSlotsSystem _exclusive = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PlumbingFilterSystem _filter = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    private EntityQuery<SolutionTransferComponent> _transferQuery;

    public override void Initialize()
    {
        base.Initialize();
        _transferQuery = GetEntityQuery<SolutionTransferComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PlumbingPumpComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;
            TryPump((uid, comp));
        }
    }

    private void TryPump(Entity<PlumbingPumpComponent> ent)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        // pump does nothing unless both slots are linked
        if (_exclusive.GetInputSlot(ent)?.GetSolution() is not {} inputEnt
            || _exclusive.GetOutputSlot(ent)?.GetSolution() is not {} outputEnt)
            return;

        var filter = _filter.GetFilteredReagent(ent);

        var input = inputEnt.Comp.Solution;
        var output = outputEnt.Comp.Solution;
        var limit = _transferQuery.Comp(ent).TransferAmount;

        var inputLimit = input.Volume;
        if (filter != null)
            inputLimit = input.GetTotalPrototypeQuantity(filter.Value);
        var amount = FixedPoint2.Min(inputLimit, limit);
        if (output.MaxVolume > FixedPoint2.Zero)
            amount = FixedPoint2.Min(amount, output.AvailableVolume);
        if (amount <= FixedPoint2.Zero)
            return;

        var newInput = input.Volume - amount;
        var newOutput = output.Volume + amount;
        var split = filter != null
            ? input.SplitSolutionWithOnly(amount, filter.Value)
            : input.SplitSolution(amount);
        DebugTools.Assert(split.Volume == amount);
        DebugTools.Assert(input.Volume == newInput);
        output.AddSolution(split, ProtoMan);
        DebugTools.Assert(output.Volume == newOutput);
    }
}
