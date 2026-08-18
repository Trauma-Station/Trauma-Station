// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Heretic.Components;

/// <summary>
/// Applies entity effects to contacting entities every second
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectContactsComponent : Component
{
    [DataField(required: true)]
    public string Id;

    [DataField(required: true)]
    public EntityEffect[] Effects;

    [DataField]
    public EntityCondition[]? Conditions;
}

[NetworkedComponent, RegisterComponent]
public sealed partial class EntityEffectContactsAffectedComponent : Component
{
    [DataField]
    public Dictionary<string, EntityUid> Contacts = new();
}
