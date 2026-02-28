// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.MartialArts;

/// <summary>
/// Capeoria specific component for doing speed stuff.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FastSpeedComponent : Component
{
    /// <summary>
    /// Speed increase and whatnot. 1.0 is normal speed, 2.0 is double speed, etc. You can do something really funny and make it really slow arts.
    /// </summary>
    [DataField(required: true)]
    public float SpeedModifier = 1.0f;

    /// <summary>
    /// Makes it so that when you scale when you have less speed, and hit weaker when you have more.
    /// </summary>
    [DataField]
    public bool InvertSpeed;
}
