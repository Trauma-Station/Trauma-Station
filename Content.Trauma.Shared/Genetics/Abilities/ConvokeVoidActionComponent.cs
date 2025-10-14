using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Component for action entities, letting it use <see cref="ConvokeVoidActionEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConvokeVoidActionComponent : Component
{
    // TODO
}

public sealed partial class ConvokeVoidActionEvent : InstantActionEvent;
