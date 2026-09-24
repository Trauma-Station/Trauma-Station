// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Trauma.Common.Wizard;

namespace Content.Trauma.Shared.Wizard.ScryingOrb;

public abstract partial class SharedScryingOrbSystem : CommonScryingOrbSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityQuery<ScryingOrbComponent> _query = default!;

    public override bool IsScryingOrbEquipped(EntityUid uid)
    {
        if (_hands.EnumerateHeld(uid).Any(held => _query.HasComponent(held)))
            return true;

        var enumerator = _inventory.GetSlotEnumerator(uid);
        while (enumerator.MoveNext(out var container))
        {
            if (_query.HasComp(container.ContainedEntity))
                return true;
        }

        return false;
    }
}
