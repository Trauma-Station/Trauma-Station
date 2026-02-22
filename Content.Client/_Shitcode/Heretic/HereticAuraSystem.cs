using Content.Shared._Shitcode.Heretic.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Shitcode.Heretic;

public sealed class HereticAuraSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticAuraComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HereticAuraComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<HereticAuraComponent> ent, ref ComponentShutdown args)
    {
        var (uid, _) = ent;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), HereticAuraKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((uid, sprite), layer);
    }

    private void OnStartup(Entity<HereticAuraComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), HereticAuraKey.Key, out _, false))
            return;

        var layer = _sprite.AddLayer((uid, sprite), comp.Sprite);
        _sprite.LayerMapSet((uid, sprite), HereticAuraKey.Key, layer);
    }
}
