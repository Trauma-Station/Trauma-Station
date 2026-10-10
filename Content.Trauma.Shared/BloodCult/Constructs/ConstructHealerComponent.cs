// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.BloodCult.Constructs;

/// <summary>
/// Marker component for the cult artificer so it can heal other constructs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConstructHealerComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> Healing = new()
    {
        { "Brute", 2.5 },
        { "Burn", 2.5 },
    };
}
