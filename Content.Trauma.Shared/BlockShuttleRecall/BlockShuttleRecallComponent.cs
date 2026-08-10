// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BlockShuttleRecall;

/// <summary>
/// This is used for blocking abductors and other antags from recalling the shuttle
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlockShuttleRecallComponent : Component
{
    /// <summary>
    /// How long to shock the abductor for when they try to interact with the comms console.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ShockTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How much to damage the abductor by when they try to interact with the comms console.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ShockDamage = 30;
}
