// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vampires.Haemomancer;

/// <summary>
/// Handles opening the UI and listing all alive crew via the action.
/// If you click on a crew member, it will notify you with their location.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActionPredatorSensesComponent : Component
{
    /// <summary>
    /// The total damage of the target required to show a "this target is wounded" popup.
    /// </summary>
    [DataField]
    public float TotalDamage = 60f;
}
