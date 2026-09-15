// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components;
using Content.Trauma.Shared.Weapons.Bombs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Weapons.Bombs;

public sealed partial class ExplosiveBombCollarSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private BodyPartSystem _bodyPart = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ExplosiveBombCollarComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ApplyHeadDamageAt == null)
                continue;

            if (_timing.CurTime < comp.ApplyHeadDamageAt)
                continue;

            comp.ApplyHeadDamageAt = null;
            TryApplyHeadDamage(uid, comp);
        }
    }

    /// <summary>
    /// When the collar is triggered, wait for trigger delay.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTrigger(Entity<ExplosiveBombCollarComponent> ent, ref TriggerEvent args)
    {
        if (ent.Comp.ApplyHeadDamageAt != null)
            return;

        TimeSpan delay = TimeSpan.FromSeconds(5);

        if (TryComp<TimerTriggerComponent>(ent, out var timer))
            delay = timer.Delay;

        ent.Comp.ApplyHeadDamageAt = _timing.CurTime + delay;
    }

    private void TryApplyHeadDamage(EntityUid uid, ExplosiveBombCollarComponent comp)
    {
        if (comp.NeckSlotHeadDamage == null)
            return;

        if (!_inventory.TryGetContainingSlot((uid, null, null), out var slot) || slot.Name != "neck")
            return;

        EntityUid wearer;
        if (_containers.TryGetContainingContainer(uid, out var container))
            wearer = container.Owner;
        else
            wearer = Transform(uid).ParentUid;

        if (!TryComp<BodyComponent>(wearer, out var body))
            return;

        var head = _bodyPart.FindBodyPart((wearer, body), BodyPartType.Head);
        if (head == null)
            return;

        _damageable.TryChangeDamage(head.Value.Owner, comp.NeckSlotHeadDamage);
    }
}
