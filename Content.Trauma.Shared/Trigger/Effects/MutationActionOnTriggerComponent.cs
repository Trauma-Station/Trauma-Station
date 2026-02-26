using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger.Effects;

/// <summary>
/// Mutation component to perform a <c>ActionMutationComponent</c> instant action when this mutation entity gets triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MutationActionOnTriggerSystem))]
[AutoGenerateComponentState]
public sealed partial class MutationActionOnTriggerComponent : BaseXOnTriggerComponent;
