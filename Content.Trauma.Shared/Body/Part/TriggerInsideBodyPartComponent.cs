// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Components.Triggers;

namespace Content.Trauma.Shared.Body.Part;

/// <summary>
/// Triggers after a certain time of being inside a body part cavity.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TriggerInsideBodyPartSystem))]
[AutoGenerateComponentState]
public sealed partial class TriggerInsideBodyPartComponent : BaseTriggerOnXComponent
{
    [DataField]
    public TimeSpan Delay;

    /// <summary>
    /// Optional popup shown when you enter the bodypart.
    /// Gets passed "delay" as <see cref="Delay"/>'s TotalSeconds.
    /// </summary>
    [DataField]
    public LocId? Popup;
}
