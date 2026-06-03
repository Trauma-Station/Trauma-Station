using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.NodeContainer;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Shared.Plumbing.Components;

namespace Content.Trauma.Server.Plumbing.Systems;

public sealed partial class FluidSinkSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FluidSinkComponent, PlumbingDeviceUpdateEvent>(OnSinkUpdate);
    }

    private void OnSinkUpdate(Entity<FluidSinkComponent> ent, ref PlumbingDeviceUpdateEvent args)
    {
        var comp = ent.Comp;

        if (!TryComp<NodeContainerComponent>(ent, out var container))
            return;

        if (!container.Nodes.TryGetValue(comp.InletName, out var inletNode) || inletNode is not PlumbingNode inletP || inletP.PipeNet is not PlumbingNet inletNet)
            return;

        if (!container.Nodes.TryGetValue(comp.OutletName, out var outletNode) || outletNode is not PlumbingNode outletP || outletP.PipeNet is not PlumbingNet outletNet)
            return;

        if (!_solution.TryGetSolution(ent.Owner, comp.InletName, out var inputNullable) || inputNullable is not { } input)
            return;

        if (!_solution.TryGetSolution(ent.Owner, comp.OutletName, out var outputNullable) || outputNullable is not { } output)
            return;

        var inputTransfer = args.FrameTime * comp.FlowRate;
        var outputTransfer = args.FrameTime * comp.DrainRate;

        _solution.TryTransferSolution(input, inletNet.Liquid, inputTransfer);
        outletNet.Liquid.AddSolution(output.Comp.Solution.SplitSolution(outputTransfer), _proto);
        _solution.UpdateChemicals(output);
    }
}
