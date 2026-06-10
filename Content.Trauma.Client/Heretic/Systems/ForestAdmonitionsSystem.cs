// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Sprite;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Side;
using Robust.Client.Player;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class ForestAdmonitionsSystem : SharedForestAdmonitionsSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private CommonSpriteVisibilitySystem _spriteVis = default!;


    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } player)
            return;

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<ForestAdmonitionsEntityComponent, ShadowCloakEntityComponent>();
        while (query.MoveNext(out var uid, out var comp, out var shadow))
        {
            if (comp.NextUpdate > now)
                continue;

            comp.NextUpdate = now + comp.UpdateTime;

            if (!Exists(shadow.User))
                continue;

            var viewer = shadow.User.Value == player ? uid : player;

            var factor = CalculateVisibilityFactor((uid, comp), viewer);
            _spriteVis.UpdateVisibilityModifiers(uid, nameof(ForestAdmonitionsComponent), factor);
        }
    }
}
