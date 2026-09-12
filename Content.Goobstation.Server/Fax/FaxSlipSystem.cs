// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Fax;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Fax;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Fax.Components;
using Content.Shared.Lube;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.Fax;

public sealed partial class FaxSlipSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private FaxSystem _fax = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnGettingFaxedSent(Entity<FaxSlipComponent> ent, ref GettingFaxedSentEvent args)
    {
        var chance = ent.Comp.LubedChance is { } lubedChance && HasComp<LubedComponent>(ent)
            ? lubedChance
            : ent.Comp.SlipChance;
        var shouldSlip = _random.Prob(chance);

        // FaxSystem wasn't really intended to do this so this copypastes logic from Send()
        if (!shouldSlip)
            return;

        // stop normal faxing behaviors
        args.Handled = true;

        // FaxSystem should probably be changed to handle this by itself
        var fax = args.Fax.Comp;
        if (fax.SendTimeoutRemaining > 0 ||
            fax.PaperSlot.Item is not { } sent ||
            fax.DestinationFaxAddress is not { } dest ||
            !fax.KnownFaxes.TryGetValue(dest, out var faxName))
            return;

        var payload = new FaxEntityPayload(sent);
        _deviceNetwork.SendPacket(args.Fax.Owner, fax.DestinationFaxAddress, ref payload);

        var actor = args.Actor;
        if (actor.IsValid())
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{actor:actor} sent entity {sent} from '{fax.FaxName}' {fax:fax} to '{faxName}' ({fax.DestinationFaxAddress})");

        fax.SendTimeoutRemaining += fax.SendTimeout;

        _audio.PlayPvs(fax.SendSound, args.Fax);
    }

    [SubscribeLocalEvent]
    private void OnLubedInsertAttempt(Entity<FaxSlipComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (!HasComp<LubedComponent>(ent))
            return;

        if (ent.Comp.LubedChance != null && HasComp<FaxMachineComponent>(args.Container.Owner))
            args.Cancel(); // too slippery to fax...
    }

    [SubscribeLocalEvent]
    private void OnSendEntity(Entity<FaxMachineComponent> ent, ref DeviceNetworkPacketEvent<FaxEntityPayload> args)
    {
        var item = args.Data.Item;
        var coords = Transform(ent).Coordinates;
        var xform = Transform(item);
        _transform.SetCoordinates((item, xform, MetaData(item)), coords);
        _container.AttachParentToContainerOrGrid((item, xform));
        _fax.Receive(ent, null, args.SenderAddress);
    }
}

[DataRecord]
public partial record struct FaxEntityPayload(EntityUid Item) : INetworkPayload;
