// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Traumas;

namespace Content.Medical.Shared.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTraumaPresentConditionComponent : Component
{
    [DataField("trauma")]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    [DataField]
    public bool Inverted;
}
