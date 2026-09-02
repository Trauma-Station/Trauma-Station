// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Vampires.Objectives;

/// <summary>
/// Objective component that checks how much blood we have sucked as a vampire.
/// </summary>
[RegisterComponent]
public sealed partial class VampireBloodConditionComponent : Component
{
    /// <summary>
    /// The total blood we have.
    /// </summary>
    [DataField]
    public int Blood;
}
