// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Audio;
using Content.Server.Power.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Trauma.Shared.Syndicate;
using Content.Trauma.Shared.Syndicate.Components;
using Content.Shared.Power;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Syndicate;

/// <inheritdoc/>
public sealed partial class SyndicateConverterSystem : SharedSyndicateConverterSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AmbientSoundSystem _ambientSound = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    [SubscribeLocalEvent]
    private void OnStartPack(Entity<SyndicateConverterComponent> converterEntity, ref SyndicateConverterStartPackBuiMessage args)
    {
        var (uid, comp) = converterEntity;
        if (!this.IsPowered(converterEntity, EntityManager) || comp.Converting)
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
        comp.ConversionEndTime = _timing.CurTime + itemEntity.Comp.ConversionTime;
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

        Spawn(itemProto, Transform(converterEntity).Coordinates);
        Del(item);
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
