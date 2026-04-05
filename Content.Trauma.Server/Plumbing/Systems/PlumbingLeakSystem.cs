using Content.Server.Fluids.EntitySystems;
using Content.Shared.NodeContainer;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Server.Plumbing.Components;

namespace Content.Trauma.Server.Plumbing.Systems;

public sealed partial class PlumbingLeakSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingDeviceComponent, PlumbingDeviceUpdateEvent>(OnLeak);
    }
    private void OnLeak(Entity<PlumbingDeviceComponent> ent, ref PlumbingDeviceUpdateEvent args)
    {
        var (uid, vent) = ent;

        if (!TryComp<NodeContainerComponent>(uid, out var container))
            return;

        foreach (var node in container.Nodes.Values)
        {

            if (node is not PlumbingNode pNode || pNode.PipeNet is not { } || !pNode.IsLeaking)
                continue;

            var net = pNode.PipeNet;

            var pressure = net.Liquid.MaxVolume > 0 ? (float) (net.Liquid.Volume / net.Liquid.MaxVolume) : 0f;

            if (pressure <= 0)
                continue;

            var leakRate = pressure * pNode.Volume * 0.2f * args.FrameTime;
            var spill = net.Liquid.SplitSolution(leakRate);

            if (spill.Volume > 0)
                _puddle.TrySpillAt(uid, spill, out _, false); // would love sound, but it spams the shit so much lmao.
        }
    }
}
