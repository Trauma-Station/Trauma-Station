// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.GhostColor;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.GhostColor;

public sealed class GhostColorSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var defaultColor = Color.FromHex("#FFFFFF88");
        var colors = EntityQueryEnumerator<GhostColorComponent, SpriteComponent>();
        while (colors.MoveNext(out var color, out var sprite))
        {
            sprite.Color = color.Color ?? defaultColor;
        }
    }
}
