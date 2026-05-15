// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Footprints;

/// <summary>
/// Mop component to clean up cleanable decals in a radius around the clicked tile.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FloorCleanerComponent : Component
{
    /// <summary>
    /// The radius to find decals in to be removed.
    /// </summary>
    [DataField]
    public float Radius = 1;

    /// <summary>
    /// Sound to play when cleaning decals.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg",
        AudioParams.Default.WithVolume(-4f));
}
