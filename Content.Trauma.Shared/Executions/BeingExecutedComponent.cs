// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._Shitmed.Targeting;

namespace Content.Trauma.Shared.Executions;

/// <summary>
/// Added to the victim of an execution while it is being "shot" to increase damage.
/// No networking or persistence since it's only added for less than 1 tick.
/// </summary>
[RegisterComponent, Access(typeof(GunExecutionSystem))]
public sealed partial class BeingExecutedComponent : Component
{
    public float Modifier;

    public TargetBodyPart? TargetPart = TargetBodyPart.Head;
}
