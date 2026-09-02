// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.GhostColor;

namespace Content.Trauma.Client.GhostColor;

public sealed partial class GhostColorSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static Color DefaultColor = Color.FromHex("#FFFFFF88");

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GhostColorComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            _sprite.SetColor((uid, sprite), comp.Color ?? DefaultColor);
        }
    }
}
