// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Skubman <ba.fallaria@gmail.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Server._EinsteinEngines.Atmos.Components;

/// <summary>
///   Component that is used on clothing to prevent ignition when exposed to a specific gas.
/// </summary>
[RegisterComponent]
public sealed partial class IgniteFromGasImmunityComponent : Component
{
    // <summary>
    //   Which body parts are given ignition immunity.
    // </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<OrganCategoryPrototype>> Parts;
}
