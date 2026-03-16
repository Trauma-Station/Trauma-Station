// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EldritchInfluenceDrainerComponent : Component
{
    [DataField]
    public float Time = 8f;

    [DataField]
    public bool Hidden;

    [DataField]
    public Dictionary<int, ProtoId<StoreCategoryPrototype>> TierToCategory = new()
    {
        { 1, "HereticPathSideT1" },
        { 2, "HereticPathSideT2" },
        { 3, "HereticPathSideT3" },
    };
}
