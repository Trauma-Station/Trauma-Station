// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Server.Plumbing.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Plumbing;

public sealed partial class PlumbingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    // A collection of all active plumbing networks
    private readonly List<PlumbingNet> _updateList = new();
    private readonly HashSet<PlumbingNet> _pipeNets = new();

    /// <summary>
    /// Adds a PlumbingNet to the processing list.
    /// Called by PlumbingNet.Initialize.
    /// </summary>
    [PublicAPI]
    public void AddPipeNet(PlumbingNet pipeNet)
    {
        _pipeNets.Add(pipeNet);
    }

    /// <summary>
    /// Removes a PlumbingNet. Called when a network is broken/deleted.
    /// </summary>
    [PublicAPI]
    public void RemovePipeNet(PlumbingNet pipeNet)
    {
        _pipeNets.Remove(pipeNet);

        // Raise an event similar to PipeNodeGroupRemovedEvent if other systems care
        var ev = new PlumbingNetRemovedEvent(pipeNet.Grid, pipeNet.NetId);
        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    /// Updates all reagent networks.
    /// Replaces the Atmos 'AtmosTick' logic.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateDevices(frameTime);
        UpdateNets(frameTime);
    }

    private void UpdateDevices(float frameTime)
    {
        var ev = new PlumbingDeviceUpdateEvent(frameTime);
        var query = EntityQueryEnumerator<PlumbingDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var device, out _))
        {
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void UpdateNets(float frameTime)
    {
        _updateList.Clear();
        _updateList.AddRange(_pipeNets);
        foreach (var pipeNet in _updateList)
        {
            // Ensure the net wasn't just deleted by a previous update in this tick
            if (_pipeNets.Contains(pipeNet))
                pipeNet.Update();
        }
    }

    /// <summary>
    /// Utility to merge a solution directly into a pipe network.
    /// Equivalent to MergeTileMixture in Atmos.
    /// </summary>
    [PublicAPI]
    public void InjectIntoNet(PlumbingNet net, Solution solution)
    {
        net.Liquid.AddSolution(solution, _proto);
    }

    /// <summary>
    ///     Removes a specific amount of solution from the pipe network.
    ///     Returns the extracted solution.
    /// </summary>
    [PublicAPI]
    public Solution RemoveFromNet(PlumbingNet net, FixedPoint2 amount)
    {
        // SplitSolution handles the math of taking a proportional
        // slice of every reagent currently in the 'Liquid' mix.
        return net.Liquid.SplitSolution(amount);
    }
}
