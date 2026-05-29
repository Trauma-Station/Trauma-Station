using Content.Shared.Weapons.Ranged.Components;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();

        SubscribeLocalEvent<BatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
    }

    private void OnAmmoCountUpdate(Entity<BatteryAmmoProviderComponent> ent, ref UpdateAmmoCounterEvent args)
    {
        if (args.Control is not BoxesStatusControl boxes)
            return;

        // <Trauma> - use ShotsFloat, CapacityFloat and FireCostMultiplier
        boxes.Update((int) (ent.Comp.ShotsFloat / args.FireCostMultiplier), (int) (ent.Comp.CapacityFloat / args.FireCostMultiplier));
        // </Trauma>
    }

    private void OnControl(Entity<BatteryAmmoProviderComponent> ent, ref AmmoCounterControlEvent args)
    {
        args.Control = new BoxesStatusControl();
    }
}
