// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryPartConditionComponent : Component
{
    [DataField]
    public HashSet<BodyPartType> Parts;

    [DataField]
    public BodyPartSymmetry? Symmetry;

    [DataField]
    public bool Inverse;
}
