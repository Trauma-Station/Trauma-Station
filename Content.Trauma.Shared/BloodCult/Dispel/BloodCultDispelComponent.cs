// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Dispel;

/// <summary>
/// Lets this entity be dispelled by a cultist using a ritual dagger.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultDispelComponent : Component
{
    [DataField(required: true)]
    public string Text = string.Empty;
}
