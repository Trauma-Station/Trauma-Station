// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Examine;

/// <summary>
/// Adds a special string for blood cultists that examine this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultExamineComponent : Component
{
    [DataField(required: true)]
    public string Text = string.Empty;
}
