using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger.Triggers;

/// <summary>
/// Triggers this entity with the user being the status effect's target.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TriggerOnStatusEffectRemovedSystem))]
[AutoGenerateComponentState]
public sealed partial class TriggerOnStatusEffectRemovedComponent : BaseTriggerOnXComponent;
