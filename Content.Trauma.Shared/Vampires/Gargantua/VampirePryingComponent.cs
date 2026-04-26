// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vampires.Gargantua;

/// <summary>
/// Component that removes <see cref="VampireComponent.UsableBlood"/> when a prying sequence succeeds.
/// </summary>
[RegisterComponent]
public sealed partial class VampirePryingComponent : Component
{
    /// <summary>
    /// Blood to remove on prying.
    /// </summary>
    [DataField]
    public int BloodToRemove = 5;
}
