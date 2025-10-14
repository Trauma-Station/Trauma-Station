using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class AutotomyMutationComponent : Component;

public sealed partial class AutotomyActionEvent : InstantActionEvent;
