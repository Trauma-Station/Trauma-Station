// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Side;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class ForestAdmonitionsSystem : SharedForestAdmonitionsSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } player)
            return;

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<ForestAdmonitionsEntityComponent, ShadowCloakEntityComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var shadow, out var sprite))
        {
            if (comp.NextUpdate > now)
                continue;

            comp.NextUpdate = now + comp.UpdateTime;

            if (!Exists(shadow.User))
                continue;

            var viewer = shadow.User.Value == player ? uid : player;

            var factor = CalculateVisibilityFactor((uid, comp), viewer);
            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(factor));
        }
    }
}
