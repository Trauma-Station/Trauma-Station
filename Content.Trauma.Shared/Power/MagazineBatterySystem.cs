using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Trauma.Shared.Power;

/// <summary>
/// Allows energy gun magazines to be used as batteries for IPC power eating.
/// </summary>
public sealed class MagazineBatterySystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagazineAmmoProviderComponent, FindBatteryEvent>(OnFindBattery);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, FindBatteryEvent>(OnFindBattery);
    }

    private void OnFindBattery(EntityUid uid, MagazineAmmoProviderComponent comp, ref FindBatteryEvent args)
    {
        if (args.FoundBattery == null ||
            // shitcode has the slot hardcoded everywhere i think so this is "fine"
            _slots.GetItemOrNull(uid, "gun_magazine") is not {} battery ||
            !TryComp<PredictedBatteryComponent>(battery, out var batteryComp))
            return;

        args.FoundBattery = (battery, batteryComp);
    }
}
