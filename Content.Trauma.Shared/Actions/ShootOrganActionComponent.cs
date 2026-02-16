// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Action component for polymorphing an organ of the performer into a projectile and shooting it at the target.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ShootOrganActionSystem))]
public sealed partial class ShootOrganActionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;
}

public sealed partial class ShootOrganActionEvent : WorldTargetActionEvent;
