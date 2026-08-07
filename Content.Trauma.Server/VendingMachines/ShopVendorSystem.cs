// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.VendingMachines;

namespace Content.Trauma.Server.VendingMachines;

public sealed partial class ShopVendorSystem : SharedShopVendorSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShopVendorComponent, TransformComponent>();
        var now = Timing.CurTime;
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            var ent = (uid, comp);
            var dirty = false;
            if (comp.Ejecting is {} ejecting && now > comp.NextEject)
            {
                Spawn(ejecting, xform.Coordinates);
                comp.Ejecting = null;
                dirty = true;
            }

            if (comp.Denying && now > comp.NextDeny)
            {
                comp.Denying = false;
                dirty = true;
            }

            if (dirty)
            {
                Dirty(uid, comp);
                UpdateVisuals(ent);
            }
        }
    }
}
