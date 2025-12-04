// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilties;

/// <summary>
/// Modifies max zoom when added and automatically zooms out.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MaxZoomMutationSystem))]
public sealed partial class MaxZoomMutationComponent : Component
{
    /// <summary>
    /// What to scale MaxZoom by.
    /// </summary>
    [DataField]
    public float Modifier = 1.25f;
}
