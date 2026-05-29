// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Components;

namespace Content.Trauma.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="KeepAliveConditionComponent"/>
/// to protect a player that is targeted to kill by another traitor
/// </summary>
[RegisterComponent]
public sealed partial class RandomTraitorTargetComponent : Component
{
}
