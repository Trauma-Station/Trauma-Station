// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;

/// <summary>
/// Abstract class used to modify common values.
/// </summary>
[RegisterComponent, NetworkedComponent]
public abstract partial class BaseAttributeComponent : Component
{

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinX;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxX;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinY;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxY;
}
