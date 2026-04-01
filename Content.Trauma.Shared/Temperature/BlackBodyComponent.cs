// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Temperature;

/// <summary>
/// Updates and supports emissive shader sprite layers using black body emission spectra.
/// Any layers using the <c>Emissive</c> shader will be used automatically.
/// Hot metal glow red, very hot metal glow white.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlackBodyComponent : Component
{
    /// <summary>
    /// Radius added to the point light at max intensity.
    /// </summary>
    [DataField]
    public float MaxLightRadius = 1f;

    /// <summary>
    /// The current color calculated by the client.
    /// </summary>
    [ViewVariables]
    public Color Color;
}

[Serializable, NetSerializable]
public enum BlackBodyVisuals : byte
{
    Temperature
}
