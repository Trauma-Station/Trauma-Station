// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Specialization.Components;

[DataDefinition]
public partial struct SpecializationStats
{
    [DataField] public int Attack;
    [DataField] public int Defense;
    [DataField] public int Speed;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SpecializationComponent : Component
{
    [DataField]
    public Dictionary<EntProtoId, SpecializationStats> WeaponSpecializations = new();
}
