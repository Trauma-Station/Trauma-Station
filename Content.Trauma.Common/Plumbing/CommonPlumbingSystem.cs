// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Trauma.Common.Plumbing;

public abstract partial class CommonPlumbingSystem : EntitySystem
{
    /// <summary>
    /// API function that hooks into some other appearance pipe logic.
    /// </summary>
    public abstract void UpdateNodeVisuals(EntityUid uid);
    public abstract bool IsPipeNode<T>(T node);

    /// <summary>
    /// API function that hooks into the underlying pipe logic.
    /// </summary>
    public abstract (PipeDirection, AtmosPipeLayer) GetAllDirectionsAndLayers<T>(Entity<TransformComponent> pipe, T node);

    /// <summary>
    /// API functions that hooks into the appearance logic.
    /// </summary>
    public abstract bool UpdateAppearance(EntityUid uid, ref HashSet<(EntityUid, AtmosPipeLayer)> connected);
}
