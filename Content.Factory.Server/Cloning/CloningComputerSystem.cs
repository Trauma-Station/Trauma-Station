// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Server.Cloning;
using Content.Server.Cloning.Components;

namespace Content.Factory.Server.Cloning;

/// <summary>
/// Handles signals for cloning console.
/// </summary>
public sealed partial class CloningConsoleSignalSystem : EntitySystem
{
    [Dependency] private CloningConsoleSystem _cloningConsole = default!;

    // can you work please? please?
    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<CloningConsoleComponent> ent, ref SignalReceivedEvent args)
    {
        if (ent.Comp.CloningPod is not {} CloningPod || ent.Comp.GeneticScanner is not {} geneticScanner)
            _cloningConsole.TryClone(ent.Owner, CloningPod, geneticScanner, consoleComponent: ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<CloningConsoleComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        if (args.Data.State != SignalState.Low || ent.Comp.CloningPod is not {} CloningPod || ent.Comp.GeneticScanner is not {} geneticScanner)
            _cloningConsole.TryClone(ent.Owner, CloningPod, geneticScanner, consoleComponent: ent.Comp);
    }
}
