// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.MartialArts;


/// <summary>
/// Tracks when an entity's breathing is blocked through Krav Maga techniques.
/// May cause suffocation damage over time when integrated with respiration systems.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockedBreathingComponent : Component
{
    [DataField]
    public TimeSpan BlockedTime = TimeSpan.Zero;
}
