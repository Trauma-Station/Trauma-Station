// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.Components;
using Content.Shared.Inventory;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Side;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed partial class VoidCloakSystem : SharedVoidCloakSystem
{
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, GetPressureProtectionValuesEvent>(OnGetPressureProtectionValues);
    }

    private void OnGetPressureProtectionValues(Entity<InventoryComponent> ent,
        ref GetPressureProtectionValuesEvent args)
    {
        if (!_inventory.TryGetSlotEntity(ent, "outerClothing", out var entity, ent.Comp))
            return;

        if (!TryComp(entity, out VoidCloakComponent? cloak) || cloak.Transparent)
            return;

        args.LowPressureMultiplier *= 1000f;
    }

    protected override void UpdatePressureProtection(EntityUid cloak, bool enabled)
    {
        base.UpdatePressureProtection(cloak, enabled);

        // This updates pressure protection in barotrauma system
        if (enabled)
            EnsureComp<PressureProtectionComponent>(cloak);
        else
            RemCompDeferred<PressureProtectionComponent>(cloak);
    }
}
