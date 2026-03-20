// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Roles;

/// <summary>
/// Mind role that runs entity effects on the mind container when this role is added/removed or the mind is transferred to a different body.
/// Can also run effects on the mind when this role is added/removed to it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RoleEffectsComponent : Component
{
    [DataField, AlwaysPushInheritance]
    public EntityEffect[] Added = [];

    [DataField, AlwaysPushInheritance]
    public EntityEffect[] Removed = [];

    [DataField, AlwaysPushInheritance]
    public EntityEffect[] MindAdded = [];

    [DataField, AlwaysPushInheritance]
    public EntityEffect[] MindRemoved = [];
}
