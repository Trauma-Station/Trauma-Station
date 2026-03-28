// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.Heretic.SpriteOverlay;

public sealed class VoidCurseOverlaySystem : SpriteOverlaySystem<VoidCurseComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoidCurseComponent, AfterAutoHandleStateEvent>((uid, comp, _) =>
            AddOverlay(uid, comp));
    }

    protected override void UpdateOverlayLayer(Entity<SpriteComponent> ent,
        VoidCurseComponent comp,
        int layer,
        EntityUid? source = null)
    {
        base.UpdateOverlayLayer(ent, comp, layer, source);

        var state = comp.Stacks >= comp.MaxLifetime ? comp.OverlayStateMax : comp.OverlayStateNormal;

        Sprite.LayerSetRsiState(ent.AsNullable(), layer, state);
    }
}
