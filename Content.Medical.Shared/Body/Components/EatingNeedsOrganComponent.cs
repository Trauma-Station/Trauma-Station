// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Body component that requires that you have an organ, head by default, to eat.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EatingNeedsOrganSystem))]
public sealed partial class EatingNeedsOrganComponent : Component
{
    [DataField]
    public ProtoId<OrganCategoryPrototype> Category = "Head";
}
