// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Abductor;

/// <summary>
/// Abductor gamerule component that stores how many experiment tasks have been done.
/// Used so both agent and scientist can greentext.
/// </summary>
[RegisterComponent]
public sealed partial class AbductorRuleComponent : Component
{
    [DataField]
    public int TasksCompleted;
}
