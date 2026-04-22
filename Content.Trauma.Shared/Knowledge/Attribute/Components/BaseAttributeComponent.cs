// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;

/// <summary>
/// Abstract component used to modify common stat values.
/// </summary>
[RegisterComponent, NetworkedComponent]
public abstract partial class BaseAttributeComponent : Component
{

    /// <summary>
    /// The minimum attribute value for the lerp (may be clampable in the future, will determine the cutoff in this case)
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinX;

    /// <summary>
    /// The maximum attribute value for the lerp (may be clampable in the future, will determine the cutoff in this case)
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxX;

    /// <summary>
    /// The minimum stat value for the lerp (i.e. what you want at low stats)
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinY;

    /// <summary>
    /// The maximum stat value for the lerp (i.e. what you want at high stats)
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxY;
}
