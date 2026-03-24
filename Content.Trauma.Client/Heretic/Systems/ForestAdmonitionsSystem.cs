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

        var query = EntityQueryEnumerator<ForestAdmonitionsEntityComponent, ShadowCloakEntityComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var shadow, out var sprite))
        {
            comp.UpdateAccumulator -= frameTime; // TODO TimeSpan

            if (comp.UpdateAccumulator > 0f)
                continue;

            comp.UpdateAccumulator = comp.UpdateTime;

            if (!Exists(shadow.User))
                continue;

            var viewer = shadow.User.Value == player ? uid : player;

            var factor = CalculateVisibilityFactor((uid, comp), viewer);
            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(factor));
        }
    }
}
