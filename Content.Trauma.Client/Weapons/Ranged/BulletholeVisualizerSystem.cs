using Content.Trauma.Shared.Weapons.Ranged;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Trauma.Client.Weapons.Ranged;

public sealed class BulletholeVisualizerSystem : VisualizerSystem<BulletholeComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    private const string BulletholeRsiPath = "/Textures/_RMC14/Effects/bulletholes.rsi";

    protected override void OnAppearanceChange(EntityUid uid, BulletholeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite
            || !AppearanceSystem.TryGetData<string>(uid, BulletholeVisuals.State, out var state, args.Component))
            return;

        var ent = (uid, sprite);

        if (!_sprite.LayerMapTryGet((uid, sprite),BulletholeVisualsLayers.Bullethole, out var layer, false))
                _sprite.LayerMapReserve(ent,BulletholeVisualsLayers.Bullethole);

        var valid = !string.IsNullOrWhiteSpace(state);

        _sprite.LayerSetVisible(ent, BulletholeVisualsLayers.Bullethole, valid);

        if (valid)
        {
            _sprite.LayerSetRsi(ent, BulletholeVisualsLayers.Bullethole, new ResPath (BulletholeRsiPath));
            _sprite.LayerSetRsiState(ent, BulletholeVisualsLayers.Bullethole, state);
        }
    }
}
