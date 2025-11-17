// <Trauma>
using Content.Shared._White.Blocking;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.PowerCell.Components;
// </Trauma>
using Content.Shared.Emp;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedBatterySystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!; // _Trauma

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(Entity<BatteryComponent> entity, ref EmpPulseEvent args)
    {
        args.Affected = true;
        // <Goob> - rechargeable blocking system handles it
        if (!HasComp<RechargeableBlockingComponent>(entity))
            args.Disabled = true;
        // </Goob>
        UseCharge(entity, args.EnergyConsumption, entity.Comp);
        // Apply a cooldown to the entity's self recharge if needed to avoid it immediately self recharging after an EMP.
        TrySetChargeCooldown(entity);
    }

    public virtual float UseCharge(EntityUid uid, float value, BatteryComponent? battery = null)
    {
        return 0f;
    }

    public virtual void SetMaxCharge(EntityUid uid, float value, BatteryComponent? battery = null) { }

    public virtual float ChangeCharge(EntityUid uid, float value, BatteryComponent? battery = null)
    {
        return 0f;
    }

    /// <summary>
    /// Checks if the entity has a self recharge and puts it on cooldown if applicable.
    /// </summary>
    public virtual void TrySetChargeCooldown(EntityUid uid, float value = -1) { }

    public virtual bool TryUseCharge(EntityUid uid, float value, BatteryComponent? battery = null)
    {
        return false;
    }

    /// <summary>
    /// Trauma - Gets the battery for an entity either if it is a battery, or from its power cell if it has a slot.
    /// </summary>
    public Entity<BatteryComponent>? GetBattery(EntityUid uid)
    {
        if (TryComp<BatteryComponent>(uid, out var battery))
            return (uid, battery);

        // not a battery and no slot found
        if (!TryComp<PowerCellSlotComponent>(uid, out var slotComp) ||
            _slots.GetItemOrNull(uid, slotComp.CellSlotId) is not {} cell)
            return null;

        return GetBattery(cell);
    }
}
