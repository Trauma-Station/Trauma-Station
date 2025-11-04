using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Trigger.Triggers;

/// <summary>
/// Triggers when this entity is directly flashed or area flashed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TriggerOnFlashedSystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class TriggerOnFlashedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Probability of being triggered when flashed.
    /// </summary>
    [DataField]
    public float Prob = 1f;
}
