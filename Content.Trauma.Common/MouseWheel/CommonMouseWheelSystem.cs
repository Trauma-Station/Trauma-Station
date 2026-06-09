// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.MouseWheel;

public abstract class CommonMouseWheelSystem : EntitySystem
{
    public abstract void HandleMouseWheel(Vector2 delta);
}
