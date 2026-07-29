// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.StationRadio.Components;
using Content.Goobstation.Shared.StationRadio.Events;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.Communications;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Radio.Components;
using Content.Server.Chat.Systems;

namespace Content.Goobstation.Server.StationRadio;

/// <summary>
/// System that handles spawning game rules when vinyl disks finish playing.
/// </summary>
public sealed partial class VinylSummonRuleSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StationSystem _station = default!;

    private const string ContainerID = "vinyl";
    private static readonly EntProtoId Ash = "Ash";

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<VinylPlayerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != ContainerID)
            return;

        // Check if vinyl player is on a station
        if (_station.GetOwningStation(ent) == null)
        {
            _popup.PopupEntity(Loc.GetString("vinyl-popout-no-station"), ent, PopupType.Medium);
            args.Cancel();
            return;
        }

        // Check if vinyl player is powered
        if (!_power.IsPowered(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("vinyl-popout-no-power"), ent, PopupType.Medium);
            args.Cancel();
            return;
        }

        // Check if vinyl player is connected to the radio system
        if (!CheckForRadioConnection(ent))
        {
            _popup.PopupEntity(Loc.GetString("vinyl-popout-no-radio-connection"), ent, PopupType.Medium);
            args.Cancel();
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnVinylInserted(Entity<VinylPlayerComponent> ent, ref VinylInsertedEvent args)
    {
        var vinyl = args.Vinyl;
        if (CompOrNull<VinylComponent>(vinyl)?.Song is not { } song)
            return;

        // Get the audio length
        var resolved = _audio.ResolveSound(song);
        var audioLength = _audio.GetAudioLength(resolved);
        var endTime = _timing.CurTime + audioLength;

        // Track this vinyl with its player
        var active = EnsureComp<ActiveVinylComponent>(vinyl);
        active.EndTime = endTime;
        active.Player = ent;
    }

    [SubscribeLocalEvent]
    private void OnVinylRemoved(Entity<VinylPlayerComponent> ent, ref VinylRemovedEvent args)
    {
        // Stop tracking if the vinyl is removed
        RemComp<ActiveVinylComponent>(args.Vinyl);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveVinylComponent>();
        foreach (var vinyl in query)
        {
            // Check if the player still exists
            var player = vinyl.Comp.Player;
            if (!Exists(player))
            {
                RemCompDeferred(vinyl, vinyl.Comp);
                continue;
            }

            // Check if vinyl player is still on a station
            if (_station.GetOwningStation(player) == null)
            {
                RemCompDeferred(vinyl, vinyl.Comp);
                _popup.PopupEntity(Loc.GetString("vinyl-popout-no-station"), player, PopupType.Medium);
                EjectVinyl(vinyl);
                continue;
            }

            // Check if vinyl player is still powered
            if (!_power.IsPowered(player))
            {
                RemCompDeferred(vinyl, vinyl.Comp);
                _popup.PopupEntity(Loc.GetString("vinyl-popout-no-power"), player, PopupType.Medium);
                EjectVinyl(vinyl);
                continue;
            }

            // Check if vinyl player is still connected to the radio system
            if (!CheckForRadioConnection(player))
            {
                RemCompDeferred(vinyl, vinyl.Comp);
                _popup.PopupEntity(Loc.GetString("vinyl-popout-no-radio-connection"), player, PopupType.Medium);
                EjectVinyl(vinyl);
                continue;
            }

            // Check if playback has finished
            if (now >= vinyl.Comp.EndTime)
            {
                HandleVinylFinished(vinyl);
                RemCompDeferred(vinyl, vinyl.Comp);
            }
        }
    }

    private void EjectVinyl(EntityUid vinyl)
    {
        _container.TryRemoveFromContainer(vinyl);
    }

    private void HandleVinylFinished(EntityUid vinylUid)
    {
        if (!TryComp<VinylSummonRuleComponent>(vinylUid, out var summonComp))
            return;

        // Resolve the game rule ID and get the threat prototype if available
        var ruleId = ResolveGameRule(summonComp.GameRule, out var threat);

        if (ruleId != null)
        {
            _ticker.StartGameRule(ruleId, out _);

            // If we have a threat prototype with an announcement, send it
            if (threat != null)
                _chat.DispatchGlobalAnnouncement(Loc.GetString(threat.Announcement), playSound: true, colorOverride: Color.Red);
        }

        var vinylXform = Transform(vinylUid);
        var vinylCoords = vinylXform.Coordinates;

        // Remove from container
        if (_container.TryGetContainingContainer((vinylUid, vinylXform, null), out var container))
            _container.Remove(vinylUid, container);

        // Play sound effect
        _audio.PlayPvs(summonComp.BurnSound, vinylCoords, AudioParams.Default.WithVolume(-5f));

        // Spawn ash at the vinyl's location
        Spawn(Ash, vinylCoords);

        // Delete the vinyl
        QueueDel(vinylUid);
    }

    private string? ResolveGameRule(string gameRuleIdentifier, out NinjaHackingThreatPrototype? threat)
    {
        threat = null;

        // Check if it's a weighted random pool
        if (ProtoMan.TryIndex<WeightedRandomPrototype>(gameRuleIdentifier, out var weightedPool))
        {
            // Pick a random threat ID from the weighted pool
            var threatId = weightedPool.Pick(_random);

            // Look up the threat prototype to get the actual game rule ID
            if (ProtoMan.TryIndex<NinjaHackingThreatPrototype>(threatId, out threat))
                return threat.Rule;

            return null;
        }

        // Assume it's a direct game rule entity ID
        return gameRuleIdentifier;
    }

    private bool CheckForRadioConnection(EntityUid uid)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return false;

        foreach (var linkedRig in source.LinkedPorts.Keys)
        {
            // Check if the radio rig is connected.
            if (!HasComp<RadioRigComponent>(linkedRig)
                || !TryComp<DeviceLinkSinkComponent>(linkedRig, out var sink))
                continue;

            // Check if the radio server is connected.
            foreach (var linkedServer in sink.LinkedSources)
            {
                if (!TryComp<StationRadioServerComponent>(linkedServer, out var _)
                    || !_power.IsPowered(linkedServer))
                    continue;

                return true;
            }
        }

        return false;
    }
}
