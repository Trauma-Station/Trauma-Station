// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.MartialArts;

namespace Content.Trauma.Shared.Areas;

public sealed partial class MartialArtAreaSystem : EntitySystem
{
    [Dependency] private AreaSystem _area = default!;

    [SubscribeLocalEvent]
    private void OnComboAttempt(Entity<AreaMartialArtComponent> ent, ref ComboAttemptEvent args)
    {
        Log.Debug($"Trying a combo in {ToPrettyString(_area.GetArea(ent))}");
        args.Cancelled |= _area.GetArea(ent) is not { } area ||
            Prototype(area) is not {} id ||
            !ent.Comp.Areas.Contains(id);
        Log.Debug($"Cancelled: {args.Cancelled}");
    }
}
