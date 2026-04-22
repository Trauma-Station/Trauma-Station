// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Proficiency.Components;

/// <summary>
/// The proficiency of the item in question.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProficiencyUsageComponent : Component
{
    [DataField]
    public EntProtoId Proficiency;
}
