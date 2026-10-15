// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.BloodCult.BloodRites;

/// <summary>
/// Stores blood that can be drained by blood rites
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodOrbComponent : Component
{
    /// <summary>
    /// Total blood stored in the orb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 StoredBlood;
}
