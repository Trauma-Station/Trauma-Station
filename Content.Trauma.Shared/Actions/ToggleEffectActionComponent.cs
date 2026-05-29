// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Component that runs effects on toggle and off toggle for actions.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ToggleEffectActionComponent : Component
{
    /// <summary>
    /// Effects to run when this action is toggled
    /// </summary>
    [DataField]
    public EntityEffect[]? OnToggle;

    /// <summary>
    /// Conditions to run before toggling the <see cref="OnToggle"/> effects.
    /// </summary>
    [DataField]
    public EntityCondition[]? OnToggleConditions;

    /// <summary>
    /// Effects to run when this action gets un-toggled
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] OffToggle = default!;

    /// <summary>
    /// Whether the action is toggled, or not.
    ///
    /// Exists to fix mispredicts caused by modifying <see cref="ActionComponent.Toggled"/> directly via the action event.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Toggled;
}

public sealed partial class EffectToggleActionEvent : InstantActionEvent;
