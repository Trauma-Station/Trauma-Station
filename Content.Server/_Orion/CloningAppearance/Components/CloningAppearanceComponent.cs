// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.CloningAppearance.Components;

//
// License-Identifier: AGPL-3.0-or-later
//

[RegisterComponent]
public sealed partial class CloningAppearanceComponent : Component
{
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear;

    [DataField]
    public bool CopyTraits;
}
