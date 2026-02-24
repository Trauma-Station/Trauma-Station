// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryOrganConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    [DataField]
    public bool Inverse;

    [DataField]
    public bool Reattaching;
}
