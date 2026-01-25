// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Shared.Executions;

/// <summary>
/// Raised on a gun used for execution to allow it to change how much damage will be dealt.
/// </summary>
[ByRefEvent]
public record struct ModifyExecutionDamageEvent(float Modifier);
