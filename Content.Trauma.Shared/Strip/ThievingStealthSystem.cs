// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Shared.Strip.Components;
using Content.Trauma.Common.Strip;

namespace Content.Trauma.Shared.Strip;

public sealed class ThievingStealthSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.SubscribeWithRelay<ThievingComponent, ThievingStealthCheckEvent>(OnStealthCheck);
    }

    private void OnStealthCheck(EntityUid uid, ThievingComponent component, ref ThievingStealthCheckEvent args)
    {
        args.Stealthy |= component.Stealthy;
    }
}
