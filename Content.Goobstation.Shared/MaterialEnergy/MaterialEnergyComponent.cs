// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 yglop <95057024+yglop@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
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
