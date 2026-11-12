// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.AlertLevel;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using Content.Trauma.Shared.Syndicate;
using Content.Trauma.Shared.Syndicate.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Syndicate;

/// <inheritdoc/>
public sealed partial class SyndicateConverterSystem : SharedSyndicateConverterSystem
{
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AmbientSoundSystem _ambientSound = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    [SubscribeLocalEvent]
    private void OnStartPack(Entity<SyndicateConverterComponent> converterEntity, ref SyndicateConverterStartPackBuiMessage args)
    {
        var (uid, comp) = converterEntity;
        if (!this.IsPowered(converterEntity, EntityManager) || comp.Converting)
            return;

        if (!TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver))
            return;

        if (!_itemSlots.TryGetSlot(uid, comp.SlotId, out var itemSlot) || itemSlot.Item is not { } item)
            return;

        if (!TryComp<SyndicateConvertibleComponent>(item, out var itemComp))
            return;

        var itemEntity = new Entity<SyndicateConvertibleComponent>(item, itemComp);

        if (!TryGetConversionCost(converterEntity, itemEntity, out var cost))
            return;

        if (!MaterialStorage.CanChangeMaterialAmount(uid, cost))
            return;

        _itemSlots.SetLock(uid, comp.SlotId, true);
        comp.Converting = true;

        if (itemEntity.Comp.ConversionTimeOverride is not null)
            comp.ConversionEndTime = _timing.CurTime + (itemEntity.Comp.ConversionTimeOverride * converterEntity.Comp.ConversionTimeScale);
        else
            comp.ConversionEndTime = _timing.CurTime + (TimeSpan.FromSeconds(15) * Math.Max(itemEntity.Comp.AlertValue, 1) * converterEntity.Comp.ConversionTimeScale);

        apcPowerReceiver.Load = comp.ActivePowerLoad;
        Appearance.SetData(uid, SyndicateConverterVisuals.Packing, true);
        _ambientSound.SetAmbience(uid, true);
        Dirty(uid, comp);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<SyndicateConverterComponent> converterEntity, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;
        FinishConverting(converterEntity, true);
    }

    private void FinishConverting(Entity<SyndicateConverterComponent> converterEntity, bool interrupted)
    {
        var (uid, comp) = converterEntity;

        _itemSlots.SetLock(uid, comp.SlotId, false);
        comp.Converting = false;
        Appearance.SetData(uid, SyndicateConverterVisuals.Packing, false);
        _ambientSound.SetAmbience(uid, false);
        Dirty(uid, comp);

        if (interrupted)
            return;

        if (!TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver))
            return;

        if (!_itemSlots.TryGetSlot(uid, comp.SlotId, out var itemSlot) || itemSlot.Item is not { } item)
            return;

        if (!TryComp<SyndicateConvertibleComponent>(item, out var itemComp))
            return;

        var itemEntity = new Entity<SyndicateConvertibleComponent>(item, itemComp);

        if (!TryGetConversionCost(converterEntity, itemEntity, out var cost) ||
            !TryGetConvertedPrototype(itemEntity, out var itemProto))
            return;

        if (!MaterialStorage.TryChangeMaterialAmount((converterEntity, null), cost))
            return;

        IncreaseAlertness(converterEntity, itemEntity.Comp.AlertValue);
        apcPowerReceiver.Load = comp.BasePowerLoad;

        Spawn(itemProto, Transform(converterEntity).Coordinates);
        Del(item);
    }

    public void IncreaseAlertness(Entity<SyndicateConverterComponent> entity, int alertValue, bool checkAlert = true)
    {
        entity.Comp.Alertness += alertValue;
        if (checkAlert)
            CheckAlertness(entity);
        return;
    }

    public void CheckAlertness(Entity<SyndicateConverterComponent> entity)
    {
        if (entity.Comp.IsDetected &&
            entity.Comp.IsEmergency)
            return;
        var converterXform = Transform(entity);
        var stationUid = _station.GetStationInMap(converterXform.MapID);

        if (entity.Comp.IsEmergency && entity.Comp.Alertness >= 2 * entity.Comp.EmergencyAlertThreshold)
        {
            var announcement = Loc.GetString("syndicate-flatpacker-announcement-emergency-2");
            var sender = Loc.GetString("syndicate-flatpacker-announcement-emergency-2-sender");
            _chatSystem.DispatchStationAnnouncement(uid, announcement, sender, false, null, Color.Red);
            _sound.PlayGlobalOnStation(uid, "/Audio/Misc/notice1.ogg");
        }

        if (!entity.Comp.IsEmergency && entity.Comp.Alertness >= entity.Comp.EmergencyAlertThreshold)
        {
            var announcement = Loc.GetString("syndicate-flatpacker-announcement-emergency",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((entity, converterXform)))));
            var sender = Loc.GetString("syndicate-flatpacker-announcement-emergency-sender");
            _chatSystem.DispatchStationAnnouncement(entity, announcement, sender, false, null, Color.Red);

            if (stationUid != null)
                _alertLevel.SetLevel(stationUid.Value, "Red", true, false, true);

            entity.Comp.IsEmergency = true;
        }
        else if (!entity.Comp.IsDetected && entity.Comp.Alertness >= entity.Comp.DetectionAlertThreshold && entity.Comp.Alertness < entity.Comp.EmergencyAlertThreshold)
        {
            var announcement = Loc.GetString("syndicate-flatpacker-announcement-detection",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((entity, converterXform)))));
            var sender = Loc.GetString("syndicate-flatpacker-announcement-detection-sender");
            _chatSystem.DispatchStationAnnouncement(entity, announcement, sender, false, null, Color.Yellow);
            _sound.PlayGlobalOnStation(entity, "/Audio/Misc/notice1.ogg");

            if (stationUid != null)
                _alertLevel.SetLevel(stationUid.Value, "Blue", false);

            entity.Comp.IsDetected = true;
        }
        return;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SyndicateConverterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Converting)
                continue;

            if (_timing.CurTime < comp.ConversionEndTime)
                continue;

            FinishConverting((uid, comp), false);
        }
    }
}
