// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.MartialArts;

namespace Content.Trauma.Shared.Areas;

public sealed class MartialArtAreaSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AreaMartialArtComponent, ComboAttemptEvent>(OnComboAttempt);
    }

    private void OnComboAttempt(Entity<AreaMartialArtComponent> ent, ref ComboAttemptEvent args)
    {
        args.Cancelled |= _area.GetArea(ent) is not { } area ||
            Prototype(area) is not {} id ||
            !ent.Comp.Areas.Contains(id);
    }
}
