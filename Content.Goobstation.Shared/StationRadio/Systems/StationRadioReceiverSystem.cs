using Content.Goobstation.Shared.StationRadio.Components;
using Content.Goobstation.Shared.StationRadio.Events;
using Content.Trauma.Common.Audio;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Goobstation.Shared.StationRadio.Systems;

public sealed class StationRadioReceiverSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaPlayedEvent>(OnMediaPlayed);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaStoppedEvent>(OnMediaStopped);
        SubscribeLocalEvent<StationRadioReceiverComponent, ActivateInWorldEvent>(OnRadioToggle);
        SubscribeLocalEvent<StationRadioReceiverComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(EntityUid uid, StationRadioReceiverComponent comp, PowerChangedEvent args)
    {
        SetAudible(comp, comp.Active && args.Powered);
    }

    private void OnRadioToggle(EntityUid uid, StationRadioReceiverComponent comp, ActivateInWorldEvent args)
    {
        comp.Active = !comp.Active;
        Dirty(uid, comp);
        UpdateAudible((uid, comp));
    }

    private void OnMediaPlayed(EntityUid uid, StationRadioReceiverComponent comp, ref StationRadioMediaPlayedEvent args)
    {
        // client cant predict storing a long term sound
        if (_net.IsClient || _audio.PlayPredicted(args.MediaPlayed, uid, uid, comp.DefaultParams) is not {} audio)
            return;

        comp.SoundEntity = audio.Entity;
        Dirty(uid, comp);
        EnsureComp<CopyrightedAudioComponent>(audio.Entity);

        UpdateAudible((uid, comp));
    }

    private void OnMediaStopped(EntityUid uid, StationRadioReceiverComponent comp, ref StationRadioMediaStoppedEvent args)
    {
        if (comp.SoundEntity == null)
            return;

        comp.SoundEntity = _audio.Stop(comp.SoundEntity);
        Dirty(uid, comp);
    }

    private void UpdateAudible(Entity<StationRadioReceiverComponent> ent)
    {
        SetAudible(ent.Comp, ent.Comp.Active && _power.IsPowered(ent.Owner));
    }

    private void SetAudible(StationRadioReceiverComponent comp, bool audible)
    {
        if (comp.SoundEntity is {} sound)
            _audio.SetVolume(sound, audible ? comp.DefaultParams.Volume : float.NegativeInfinity);
    }
}
