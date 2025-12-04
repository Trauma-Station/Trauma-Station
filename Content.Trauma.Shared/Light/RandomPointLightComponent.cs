// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Trauma.Shared.Light;

/// <summary>
/// Randomizes the color and energy of this entity's point light on mapinit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomPointLightComponent : Component
{
    /// <summary>
    /// The possible colors to pick from.
    /// </summary>
    [DataField]
    public List<Color> Colors = new()
    {
        Color.White,
        Color.Red,
        Color.Yellow,
        Color.Green,
        Color.Blue,
        Color.Purple,
        Color.Pink
    };

    /// <summary>
    /// The min and max energy to pick from.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Energy = default!;
}
