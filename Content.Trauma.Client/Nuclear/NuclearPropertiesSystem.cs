// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Nuclear;

namespace Content.Trauma.Client.Nuclear;

public sealed partial class NuclearPropertiesSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearPropertiesComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<NuclearPropertiesComponent> ent, ref ComponentInit args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), NuclearPropertiesVisuals.Layer, out var layer, false))
            return;

        _sprite.LayerSetColor((ent, sprite), layer, ent.Comp.Color);
    }
}
