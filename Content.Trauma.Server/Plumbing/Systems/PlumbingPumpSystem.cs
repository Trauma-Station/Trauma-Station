using Content.Server.Power.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Shared.Plumbing.Components;

namespace Content.Trauma.Server.Plumbing.Systems;

public sealed class FluidPumpSystem : EntitySystem
{
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FluidPumpComponent, PlumbingDeviceUpdateEvent>(OnPumpUpdate);
    }

    private void OnPumpUpdate(Entity<FluidPumpComponent> ent, ref PlumbingDeviceUpdateEvent args)
    {
        var comp = ent.Comp;
        if (!comp.Enabled || !_power.IsPowered(ent))
            return;

        if (!TryComp<NodeContainerComponent>(ent, out var container))
            return;

        if (!container.Nodes.TryGetValue(comp.InletName, out var inletNode) || inletNode is not PlumbingNode inletP || inletP.PipeNet is not PlumbingNet inletNet)
            return;

        if (!container.Nodes.TryGetValue(comp.OutletName, out var outletNode) || outletNode is not PlumbingNode outletP || outletP.PipeNet is not PlumbingNet outletNet)
            return;

        var outletPressure = (float) (outletNet.Liquid.Volume / outletNet.Liquid.MaxVolume) + outletNet.ExternalPressureForce;
        var inletPressure = (float) (inletNet.Liquid.Volume / inletNet.Liquid.MaxVolume) + inletNet.ExternalPressureForce;

        if (outletPressure >= comp.MaxOutputPressure + inletPressure)
            return;

        var amountToMove = FixedPoint2.Min(comp.PumpRate * args.FrameTime, inletNet.Liquid.Volume);

        if (amountToMove <= 0)
            return;

        var moved = inletNet.Liquid.SplitSolution(amountToMove);
        _plumbing.InjectIntoNet(outletNet, moved);

        var forceValue = (float) (amountToMove / outletNet.Liquid.MaxVolume);
        outletNet.ExternalPressureForce += forceValue;
        inletNet.ExternalPressureForce -= forceValue;
    }
}
