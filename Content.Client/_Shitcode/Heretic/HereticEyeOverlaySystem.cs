using System.Numerics;
using Content.Shared._Shitcode.Heretic.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Shitcode.Heretic;

public sealed class HereticEyeOverlaySystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticEyeOverlayComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HereticEyeOverlayComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<HereticEyeOverlayComponent> ent, ref ComponentShutdown args)
    {
        var (uid, _) = ent;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), HereticEyeOverlayKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((uid, sprite), layer);
    }

    private void OnStartup(Entity<HereticEyeOverlayComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), HereticEyeOverlayKey.Key, out _, false))
            return;

        var layer = _sprite.AddLayer((uid, sprite), comp.Sprite);
        sprite.LayerSetShader(layer, "unshaded");
        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(0f, 0.5f));
        _sprite.LayerMapSet((uid, sprite), HereticEyeOverlayKey.Key, layer);
    }
}
