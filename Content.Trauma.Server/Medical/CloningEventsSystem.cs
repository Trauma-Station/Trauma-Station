// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Cloning;
using Content.Server.Cloning.Components;
using Content.Server.Medical.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Trauma.Common.Medical;

namespace Content.Trauma.Server.Medical;

/// <summary>
/// Connect cloning console to events.
/// Also handles scanner port connected event for completion
/// </summary>
public sealed class CloningEventsSystem : EntitySystem
{
    [Dependency] private readonly CloningConsoleSystem _cloningConsole = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedicalScannerComponent, NewLinkEvent>(OnNewLink);

        SubscribeLocalEvent<CloningConsoleComponent, ScannerConnectedEvent>(OnScannerConnected);
        SubscribeLocalEvent<CloningConsoleComponent, ScannerDisconnectedEvent>(OnScannerDisconnected);
    }

    private void OnNewLink(Entity<MedicalScannerComponent> ent, ref NewLinkEvent args)
    {
        if (args.Sink != ent.Owner || args.SinkPort != MedicalScannerComponent.ScannerPort || args.SourcePort != CloningConsoleComponent.ScannerPort)
            return;

        // cloning console does this too but there's no harm
        var ev = new ScannerConnectedEvent(ent.Owner);
        RaiseLocalEvent(args.Source, ref ev);
        ent.Comp.ConnectedConsole = args.Source;
    }

    private void OnScannerConnected(Entity<CloningConsoleComponent> ent, ref ScannerConnectedEvent args)
    {
        _cloningConsole.RecheckConnections(ent, ent.Comp.CloningPod, args.Scanner, ent);
    }

    private void OnScannerDisconnected(Entity<CloningConsoleComponent> ent, ref ScannerDisconnectedEvent args)
    {
        _cloningConsole.UpdateUserInterface(ent, ent);
    }
}
