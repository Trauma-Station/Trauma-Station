// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Systems;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.Heretic.SpriteOverlay;

public sealed class HereticCombatMarkOverlaySystem : SpriteOverlaySystem<HereticCombatMarkComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCombatMarkComponent, AfterAutoHandleStateEvent>((uid, comp, _) =>
            AddOverlay(uid, comp));
        SubscribeLocalEvent<HereticCombatMarkComponent, UpdateCombatMarkAppearanceEvent>((uid, comp, _) =>
            AddOverlay(uid, comp));
    }

    protected override int? GetLayerIndex(Entity<SpriteComponent> ent, HereticCombatMarkComponent comp)
    {
        return comp.Path == HereticPath.Cosmos ? 0 : null; // Cosmos mark should be behind the sprite
    }

    protected override void UpdateOverlayLayer(Entity<SpriteComponent> ent,
        HereticCombatMarkComponent comp,
        int layer,
        EntityUid? source = null)
    {
        base.UpdateOverlayLayer(ent, comp, layer, source);

        var state = comp.Path.ToString().ToLower();

        Sprite.LayerSetRsiState(ent.AsNullable(), layer, state);
    }
}
