// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Trigger.Triggers;

/// <summary>
/// Randomly triggers serverside with a probability rolled every second.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RandomTriggerSystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class RandomTriggerComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Probability of being triggered every second.
    /// </summary>
    [DataField(required: true)]
    public float Prob;

    /// <summary>
    /// How long to wait between each roll.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
