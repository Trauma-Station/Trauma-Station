// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Common.MartialArts;

/// <summary>
/// Sneak attack component for ninja stuff.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SneakAttackComponent : Component
{
    /// <summary>
    /// Indicates whether the player has been found.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsFound = false;

    /// <summary>
    /// Cooldown time before the player can hide again after being found.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public int SecondsTillHidden;

    /// <summary>
    /// Timestamp until player is hidden again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextHidden = TimeSpan.Zero;
}
