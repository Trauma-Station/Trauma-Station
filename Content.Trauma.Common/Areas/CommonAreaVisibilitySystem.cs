// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Areas;

/// <summary>
/// Client-only common system to set area visibility.
/// </summary>
public abstract class CommonAreaVisibilitySystem : EntitySystem
{
    public abstract void SetVisible(bool visible);
}
