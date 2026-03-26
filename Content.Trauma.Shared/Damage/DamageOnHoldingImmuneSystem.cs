// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Trauma.Common.Damage;

namespace Content.Trauma.Shared.Damage;

public sealed class DamageOnHoldingImmuneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, DamageOnHoldingAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<DamageOnHoldingImmuneComponent, DamageOnHoldingAttemptEvent>(OnDamageAttempt);
    }

    private void RelayEvent(Entity<InventoryComponent> ent, ref DamageOnHoldingAttemptEvent args)
    {
        var slots = new InventorySystem.InventorySlotEnumerator(ent, SlotFlags.GLOVES);
        while (slots.NextItem(out var item))
        {
            RaiseLocalEvent(item, ref args);
        }
    }

    private void OnDamageAttempt(Entity<DamageOnHoldingImmuneComponent> ent, ref DamageOnHoldingAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
