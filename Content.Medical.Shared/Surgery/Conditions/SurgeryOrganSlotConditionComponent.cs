// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Surgery.Conditions;

/// <summary>
/// Requires that an organ slot does (not) exist on the target part for a surgery to be possible.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryOrganSlotConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganSlot = default!;

    [DataField]
    public bool Inverse;
}
