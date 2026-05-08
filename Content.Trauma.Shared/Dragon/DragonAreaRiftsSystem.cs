// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Content.Trauma.Common.Dragon;
using Content.Trauma.Shared.Areas;

namespace Content.Trauma.Shared.Dragon;

public sealed class DragonAreaRiftsSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonAreaRiftsComponent, DragonSpawnRiftAttemptEvent>(OnSpawnRiftAttempt);
    }

    private void OnSpawnRiftAttempt(Entity<DragonAreaRiftsComponent> ent, ref DragonSpawnRiftAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_area.GetArea(ent) is not { } area ||
            _whitelist.IsWhitelistFail(ent.Comp.AreaWhitelist, area))
        {
            args.Cancelled = true;
            args.Popup = "You need to be in a station area!";
        }
    }
}
