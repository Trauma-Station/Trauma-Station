// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.Heretic.SpriteOverlay;

public sealed class HereticArenaCrownOverlaySystem : SpriteOverlaySystem<HereticArenaParticipantComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticArenaParticipantComponent, AfterAutoHandleStateEvent>((uid, comp, _) =>
            AddOverlay(uid, comp));
    }

    protected override void UpdateOverlayLayer(Entity<SpriteComponent> ent,
        HereticArenaParticipantComponent comp,
        int layer,
        EntityUid? source = null)
    {
        base.UpdateOverlayLayer(ent, comp, layer, source);

        var state = comp.IsVictor ? comp.VictorState : comp.FighterState;

        Sprite.LayerSetRsiState(ent.AsNullable(), layer, state);
    }
}
