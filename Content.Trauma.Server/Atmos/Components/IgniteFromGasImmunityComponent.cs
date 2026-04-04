// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Atmos.Components;

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
