// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.ItemSwitch;

namespace Content.Medical.Server.ItemSwitch;

public sealed class ItemSwitchSystem : SharedItemSwitchSystem
{
    // TODO SHITMED: make this use battery events not this fucking slop
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ItemSwitchComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            CheckPowerAndSwitchState((uid, comp));
        }
    }
}
