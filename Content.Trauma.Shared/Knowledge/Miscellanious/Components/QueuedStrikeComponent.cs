// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Miscellanious.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class QueuedStrikeComponent : Component
{
    /// <summary>
    /// Entity to strike.
    /// </summary>
    [DataField]
    public EntityUid Target;

    /// <summary>
    /// Time when next hit.
    /// </summary>
    [DataField]
    public TimeSpan TimeToHit;

    /// <summary>
    /// Use first offhand if possible
    /// </summary>
    [DataField]
    public bool Offhand = false;
}
