// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Lavaland.Common.Mining;

/// <summary>
///     Component that holds information about ore that hasn't been processed yet.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UnclaimedOreComponent : Component
{
    [DataField(required: true)]
    public float MiningPoints;
}
