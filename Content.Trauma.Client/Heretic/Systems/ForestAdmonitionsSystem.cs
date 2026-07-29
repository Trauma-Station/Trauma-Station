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

    [Dependency] private EntityQuery<ShadowCloakEntityComponent> _shadowQuery = default!;

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ForestAdmonitionsEntityComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _spriteVis.UpdateVisibilityModifiers(ent, nameof(ForestAdmonitionsComponent), 1f);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } player)
            return;

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<ForestAdmonitionsEntityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextUpdate > now)
                continue;

            comp.NextUpdate = now + comp.UpdateTime;

            var viewer = (_shadowQuery.CompOrNull(uid)?.User ?? uid) == player ? uid : player;

            var factor = CalculateVisibilityFactor((uid, comp), viewer);
            _spriteVis.UpdateVisibilityModifiers(uid, nameof(ForestAdmonitionsComponent), factor);
        }
    }
}
