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

    private static readonly ProtoId<SinkPortPrototype> ClonePort = "Clone";
    // can you work please? please?
    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<CloningConsoleComponent> ent, ref SignalReceivedEvent args)
    {
        Log.Info($"SignalReceivedEvent");
        Log.Info($"{args.Port}");
        Log.Info($"{ent.Comp.CloningPod}");
        Log.Info($"{ent.Comp.GeneticScanner}");
        if (args.Port != ClonePort || ent.Comp.CloningPod == null || ent.Comp.GeneticScanner == null)
            return;
        Log.Info($"SignalReceivedEvent : good");
        _cloningConsole.TryClone(ent.Owner, ent.Comp.CloningPod.Value, ent.Comp.GeneticScanner.Value, consoleComponent: ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<CloningConsoleComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        Log.Info($"SignalReceivedEvent");
        Log.Info($"{args.Port}");
        Log.Info($"{ent.Comp.CloningPod}");
        Log.Info($"{ent.Comp.GeneticScanner}");
        if (args.Port != ClonePort || args.Data.State == SignalState.Low || ent.Comp.CloningPod == null || ent.Comp.GeneticScanner == null)
            return;
        Log.Info($"LogicStatePayload : good");
        _cloningConsole.TryClone(ent.Owner, ent.Comp.CloningPod.Value, ent.Comp.GeneticScanner.Value, consoleComponent: ent.Comp);
    }
}
