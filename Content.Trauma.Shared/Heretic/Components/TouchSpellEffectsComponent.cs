// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Heretic.Components;

/// <summary>
/// Applies entity effects to the target on touch spell use, if conditions pass
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TouchSpellEffectsComponent : Component
{
    [DataField]
    public EntityEffect[]? Effects;

    [DataField]
    public EntityCondition[]? Conditions;
}
