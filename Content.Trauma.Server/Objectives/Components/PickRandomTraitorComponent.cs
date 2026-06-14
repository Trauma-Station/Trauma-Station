// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Components;

namespace Content.Trauma.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random traitor
/// If there are no traitors it will fallback to any person.
/// </summary>
[RegisterComponent]
public sealed partial class PickRandomTraitorComponent : Component
{
}
