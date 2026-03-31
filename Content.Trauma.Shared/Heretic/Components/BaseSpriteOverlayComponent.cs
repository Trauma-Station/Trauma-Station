// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Components;

public abstract partial class BaseSpriteOverlayComponent : Component
{
    public abstract Enum Key { get; set; }

    public abstract SpriteSpecifier? Sprite { get; set; }

    public virtual bool Unshaded { get; set; } = true;

    public virtual Vector2 Offset { get; set; } = Vector2.Zero;
}
