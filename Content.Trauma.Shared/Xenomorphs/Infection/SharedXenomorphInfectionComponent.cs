// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.StatusIcon;

namespace Content.Trauma.Shared.Xenomorphs.Infection;

[NetworkedComponent]
public abstract partial class SharedXenomorphInfectionComponent : Component
{
    /// <summary>
    /// A set of prototype IDs for status icons representing different growth stages of the infection.
    /// </summary>
    [DataField]
    public Dictionary<int, ProtoId<InfectionIconPrototype>> InfectedIcons = new();

    /// <summary>
    /// Current stage of infection development.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int GrowthStage;
}
