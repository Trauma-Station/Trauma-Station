// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Materials;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.MaterialEnergy;

[RegisterComponent, NetworkedComponent, Access(typeof(MaterialEnergySystem))]
public sealed partial class MaterialEnergyComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<MaterialPrototype>> MaterialWhiteList = new();
}
