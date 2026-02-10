using Content.Goobstation.Shared.StationRadio.Components;
using Content.Goobstation.Shared.StationRadio.Events;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Trauma.Common.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Goobstation.Shared.StationRadio.Systems;

public sealed class VinylPlayerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLinkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VinylPlayerComponent, EntInsertedIntoContainerMessage>(OnVinylInserted);
        SubscribeLocalEvent<VinylPlayerComponent, EntRemovedFromContainerMessage>(OnVinylRemove);
        SubscribeLocalEvent<VinylPlayerComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<VinylPlayerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(EntityUid uid, VinylPlayerComponent comp, PowerChangedEvent args)
    {
        if (comp.SoundEntity != null && !args.Powered)
            comp.SoundEntity = _audio.Stop(comp.SoundEntity);

        if (!CheckForRadioRig(uid))
            return;

        if (!args.Powered)
            StopReceivers();
    }

    private void OnDestruction(EntityUid uid, VinylPlayerComponent comp, DestructionEventArgs args)
    {
        if (!CheckForRadioRig(uid))
            return;

        StopReceivers();
    }

    private void OnVinylInserted(EntityUid uid, VinylPlayerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp(args.Entity, out VinylComponent? vinylcomp) || _net.IsClient || vinylcomp.Song == null || !_power.IsPowered(uid))
            return;

        if (_audio.PlayPredicted(vinylcomp.Song, uid, uid, AudioParams.Default.WithVolume(3f).WithMaxDistance(4.5f)) is not {} audio)
            return;

        comp.SoundEntity = audio.Entity;
        EnsureComp<CopyrightedAudioComponent>(audio.Entity);

        // Used by VinylSummonRuleSystem
        var ev = new VinylInsertedEvent(args.Entity);
        RaiseLocalEvent(uid, ref ev);

        if (!CheckForRadioRig(uid))
            return;

        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        var receiverEv = new StationRadioMediaPlayedEvent(vinylcomp.Song);
        while (query.MoveNext(out var receiver, out var receiverComponent))
        {
            if (receiverComponent.SoundEntity == null)
                RaiseLocalEvent(receiver, ref receiverEv);
        }
    }

    private void OnVinylRemove(EntityUid uid, VinylPlayerComponent comp, EntRemovedFromContainerMessage args)
    {
        if (comp.SoundEntity != null)
            comp.SoundEntity = _audio.Stop(comp.SoundEntity);

        // Used by VinylSummonRuleSystem
        var ev = new VinylRemovedEvent(args.Entity);
        RaiseLocalEvent(uid, ref ev);

        if (!CheckForRadioRig(uid))
            return;

        StopReceivers();
    }

    private void StopReceivers()
    {
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        var ev = new StationRadioMediaStoppedEvent();
        while (query.MoveNext(out var uid, out _))
        {
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private bool CheckForRadioRig(EntityUid uid)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return false;

        foreach (var linked in source.LinkedPorts.Keys)
        {
            if (HasComp<RadioRigComponent>(linked) && CheckForRadioServer(linked))
                return true;
        }

        return false;
    }

    private bool CheckForRadioServer(EntityUid uid)
    {
        if (!TryComp<DeviceLinkSinkComponent>(uid, out var source))
            return false;

        foreach (var linked in source.LinkedSources)
        {
            if (HasComp<StationRadioServerComponent>(linked))
                return true;
        }

        return false;
    }
}
