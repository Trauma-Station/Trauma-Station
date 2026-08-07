// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Temperature;

public sealed partial class ItemSlotHeaterSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedTemperatureSystem _temp = default!;
    [Dependency] private EntityQuery<TemperatureComponent> _tempQuery = default!;

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<ActiveItemSlotHeaterComponent, ItemSlotHeaterComponent>();
        while (eqe.MoveNext(out var uid, out var active, out var heat))
        {
            if (now < active.NextUpdate)
                continue;

            if (_itemSlots.GetItemOrNull(uid, heat.Slot) is not {} item)
                continue;

            if (!_tempQuery.TryComp(item, out var temp))
                continue;

            // In order to support cooling, we must check if we are heating the entity or not.
            // If we are heating, then check if we are higher than the Max Temperature set,
            // otherwise check if we are below the Max Temperature (if we are cooling).
            if ( ( heat.Temp >= 0 && temp.Temperature >= heat.MaxTemp )
                || ( heat.Temp < 0 && temp.Temperature <= heat.MaxTemp ) )
                continue;

            _temp.ChangeHeat(item, heat.Temp);

            active.NextUpdate = now + heat.Update;
            Dirty(uid, active);
        }
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<ItemSlotHeaterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // Required to not cause mispredicts
        if (_timing.ApplyingState)
            return;

        // Only apply the component if we inserted it into the correct item slot
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.Slot, out var itemSlot))
            return;

        if (itemSlot.Item is not { } item || args.Entity != item)
            return;

        EnsureComp<ActiveItemSlotHeaterComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<ItemSlotHeaterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Required to not cause mispredicts
        if (_timing.ApplyingState)
            return;

        // The item slot must be empty in order to stop the heating proccess
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.Slot) != null)
            return;

        RemCompDeferred<ActiveItemSlotHeaterComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<ItemSlotHeaterComponent> ent, ref ExaminedEvent args)
    {
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.Slot) is not { } item || !_tempQuery.TryComp(item, out var temp))
            return;

        var temperature = temp.Temperature;
        // Mispredicts but oh well
        args.PushMarkup(Loc.GetString("item-slot-heater-temp", ("temp", temperature.ToString("F1"))));
    }

}
