// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Side;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class CoinFlipSystem : SharedCoinFlipSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoinFlipComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<CoinFlipComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite ||
            !Appearance.TryGetData(ent, CoinFlipVisuals.SpriteState, out string state, args.Component))
            return;

        _sprite.LayerSetRsiState((ent, sprite), CoinFlipKey.Key, state);
    }
}
