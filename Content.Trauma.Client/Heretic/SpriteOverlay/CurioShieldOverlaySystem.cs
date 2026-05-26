// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;
using Content.Trauma.Shared.Heretic.Components.Side;

namespace Content.Trauma.Client.Heretic.SpriteOverlay;

public sealed partial class CurioShieldOverlaySystem : SpriteOverlaySystem<UnfathomableCurioShieldComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnfathomableCurioShieldComponent, AfterAutoHandleStateEvent>((uid, comp, _) =>
            AddOverlay(uid, comp));
    }
}
