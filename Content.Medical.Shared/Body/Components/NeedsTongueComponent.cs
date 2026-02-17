// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Body component that requires there is a tongue organ installed and healthy to speak.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(NeedsTongueSystem))]
public sealed partial class NeedsTongueComponent : Component
{
    [DataField]
    public ProtoId<OrganCategoryPrototype> Category = "Tongue";
}
