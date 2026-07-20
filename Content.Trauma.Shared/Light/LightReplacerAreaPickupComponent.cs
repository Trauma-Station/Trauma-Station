// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Light;

/// <summary>
/// Allows for area pickup of light bulbs around where you click.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LightReplacerAreaPickupComponent : Component
{
    [DataField]
    public float Range = 1.25f;

    /// <summary>
    /// Sound played after doing an area pickup
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/trashbag1.ogg");
}
