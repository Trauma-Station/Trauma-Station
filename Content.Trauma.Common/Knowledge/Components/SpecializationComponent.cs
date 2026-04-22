// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Stores all specializations. Because I can't think up of a better method.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpecializationComponent : Component
{

    [DataField]
    public Dictionary<EntProtoId, SpecializationStats> WeaponSpecializations = new();
}

[DataDefinition]
public partial struct SpecializationStats
{
    [DataField] public int Attack;
    [DataField] public int Defense;
    [DataField] public int Speed;
    [DataField] public int Damage;
}
