// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger.Triggers;

/// <summary>
/// Triggers when this entity is directly flashed or area flashed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TriggerOnFlashedSystem))]
[AutoGenerateComponentState]
public sealed partial class TriggerOnFlashedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Probability of being triggered when flashed.
    /// </summary>
    [DataField]
    public float Prob = 1f;
}
