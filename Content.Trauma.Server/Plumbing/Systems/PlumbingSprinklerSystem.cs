// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.NodeContainer;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Shared.Plumbing;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Plumbing.Systems;

public sealed class FluidSprinklerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FluidSprinklerComponent, PlumbingDeviceUpdateEvent>(OnUpdate);
    }

    private void OnUpdate(Entity<FluidSprinklerComponent> ent, ref PlumbingDeviceUpdateEvent args)
    {
        var (uid, comp) = ent;

        // Tmep Check
        var mixture = _atmos.GetContainingMixture(uid);
        if (mixture is { } && mixture.Temperature >= comp.ThermalActivationThreshold)
            comp.Enabled = true;

        if (!comp.Enabled)
            return;

        if (!TryComp<NodeContainerComponent>(uid, out var container) || !container.Nodes.TryGetValue(comp.InletName, out var node) || node is not PlumbingNode pNode || pNode.PipeNet is not PlumbingNet net)
            return;

        // Pressure Check
        var pressure = (net.Liquid.Volume / net.Liquid.MaxVolume) + net.ExternalPressureForce;
        if (pressure <= 0.1f)
            return;

        var amount = comp.TransferRate * pressure * args.FrameTime;
        var spray = net.Liquid.SplitSolution(amount);

        if (spray.Volume <= 0)
            return;

        // Spread liquid to a random spot within range
        var offset = _random.NextVector2(comp.SprayRange);
        var target = Transform(uid).Coordinates.Offset(offset);

        _puddle.TrySpillAt(target, spray, out _, false);
    }
}
