// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;

namespace Content.Goobstation.Client.Emoting;

public sealed partial class ShowSpriteLayerStatusEffectSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    [SubscribeLocalEvent]
    private void OnApply(Entity<ShowSpriteLayerStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        SetVisible(ent.Comp.Layer, args.Target, ent.Comp.SetVisible);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<ShowSpriteLayerStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        SetVisible(ent.Comp.Layer, args.Target, !ent.Comp.SetVisible);
    }

    private void SetVisible(Enum key, EntityUid uid, bool visible)
    {
        if (!_spriteQuery.TryComp(uid, out var sprite))
            return;

        var ent = (uid, sprite);
        if (!_sprite.TryGetLayer(ent, key, out var layer, false)) // dont care if its missing
            return;

        _sprite.LayerSetVisible(layer, visible);
    }
}
