using Content.Trauma.Shared.Weapons.Ranged;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.Weapons.Ranged;

public sealed class BulletholeVisualizerSystem : VisualizerSystem<BulletholeComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    private const string BulletholeRsiPath = "/Textures/_RMC14/Effects/bulletholes.rsi";

    protected override void OnAppearanceChange(EntityUid uid, BulletholeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, BulletholeVisuals.State, out var state, args.Component))
            return;

        var ent = (uid, sprite);

        if (!_sprite.LayerMapTryGet(ent, BulletholeVisualsLayers.Bullethole, out var layer))
            layer = _sprite.LayerMapReserve(BulletholeVisualsLayers.Bullethole);

        var valid = !string.IsNullOrWhiteSpace(state);

        _sprite.LayerSetVisible(BulletholeVisualsLayers.Bullethole, valid);

        if (valid)
        {
            _sprite.LayerSetRsi(BulletholeVisualsLayers.Bullethole, BulletholeRsiPath);
            _sprite.LayerSetRsiState(BulletholeVisualsLayers.Bullethole, state);
        }
    }
}

