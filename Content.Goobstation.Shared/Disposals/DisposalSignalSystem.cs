// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Power.EntitySystems;

namespace Content.Goobstation.Shared.Disposals;

public sealed partial class DisposalSignalSystem : EntitySystem
{
    [Dependency] private SharedDisposalUnitSystem _disposal = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    public static readonly ProtoId<SinkPortPrototype> FlushPort = "DisposalFlush";
    public static readonly ProtoId<SinkPortPrototype> EjectPort = "DisposalEject";
    public static readonly ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnSignalReceived(Entity<DisposalUnitComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == FlushPort)
            _disposal.ToggleEngage(ent);
        else if (args.Port == EjectPort)
            _disposal.EjectContents(ent);
        else if (args.Port == TogglePort)
            _power.TogglePower(ent);
    }
}
