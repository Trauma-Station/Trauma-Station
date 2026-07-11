// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.Nuclear;

namespace Content.Trauma.Server.Nuclear;

public sealed partial class NuclearMachineSystem : SharedNuclearMachineSystem
{
    [Dependency] private NodeContainerSystem _node = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearMachineComponent, GasAnalyzerScanEvent>(OnAnalyzerScan);
        SubscribeLocalEvent<NuclearMachineComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NuclearMachineComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<NuclearMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextLog is { } nextLog && now >= nextLog)
                SendLog((uid, comp));
        }
    }

    private void OnAnalyzerScan(Entity<NuclearMachineComponent> ent, ref GasAnalyzerScanEvent args)
    {
        var comp = ent.Comp;
        if (comp.InletEnt is not { } inEnt || comp.OutletEnt is not { } outEnt)
            return;

        args.GasMixtures ??= [];

        if (_node.TryGetNode(inEnt, comp.PipeName, out PipeNode? inlet) && inlet.Air.Volume != 0f)
        {
            var inletAirLocal = inlet.Air.Clone();
            inletAirLocal.Multiply(inlet.Volume / inlet.Air.Volume);
            inletAirLocal.Volume = inlet.Volume;
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-inlet"), inletAirLocal));
        }

        if (_node.TryGetNode(outEnt, comp.PipeName, out PipeNode? outlet) && outlet.Air.Volume != 0f)
        {
            var outletAirLocal = outlet.Air.Clone();
            outletAirLocal.Multiply(outlet.Volume / outlet.Air.Volume);
            outletAirLocal.Volume = outlet.Volume;
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-outlet"), outletAirLocal));
        }
    }

    private void OnShutdown(Entity<NuclearMachineComponent> ent, ref ComponentShutdown args)
    {
        DeletePipes(ent.Comp);
    }

    public bool GetPipes(Entity<NuclearMachineComponent?> ent, [NotNullWhen(true)] out PipeNode? inlet, [NotNullWhen(true)] out PipeNode? outlet)
    {
        inlet = null;
        outlet = null;

        if (!Query.Resolve(ent, ref ent.Comp, false))
            return false;

        var comp = ent.Comp;
        if (comp.InletEnt is not { } inEnt || Deleted(inEnt))
            comp.InletEnt = inEnt = SpawnAttachedTo(comp.PipePrototype, new(ent, comp.InletPos), rotation: Angle.FromDegrees(comp.InletRot));
        if (comp.OutletEnt is not { } outEnt || Deleted(outEnt))
            comp.OutletEnt = outEnt = SpawnAttachedTo(comp.PipePrototype, new(ent, comp.OutletPos), rotation: Angle.FromDegrees(comp.OutletRot));

        if (!Transform(inEnt).Anchored || !Transform(outEnt).Anchored)
        {
            _popup.PopupEntity("Invalid anchoring position!", ent, PopupType.MediumCaution);
            DeletePipes(ent.Comp);
            _transform.Unanchor(ent);
            return false;
        }

        return _node.TryGetNode(inEnt, comp.PipeName, out inlet) &&
            _node.TryGetNode(outEnt, comp.PipeName, out outlet);
    }

    private void OnAnchorChanged(Entity<NuclearMachineComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            DeletePipes(ent.Comp);
    }

    private void DeletePipes(NuclearMachineComponent comp)
    {
        QueueDel(comp.InletEnt);
        QueueDel(comp.OutletEnt);
        comp.InletEnt = null;
        comp.OutletEnt = null;
    }
}
