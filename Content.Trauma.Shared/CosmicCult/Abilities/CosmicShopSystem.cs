// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;

namespace Content.Trauma.Shared.CosmicCult.Abilities;

public sealed partial class CosmicShopSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnCosmicShop(Entity<CosmicCultComponent> ent, ref CosmicShopEvent args)
    {
        _ui.TryToggleUi(args.Action.Owner, CosmicShopKey.Key, ent);
        args.Handled = true;
    }
}
