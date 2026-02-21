// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Component for action entities that lets them spawn an item in the user's hand (or drop it) via <see cref="ConjureActionEvent"/>.
/// The action needs to raise the event on the action entity instead of the user for this to work.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConjureActionComponent : Component
{
    /// <summary>
    /// The item to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Spawn;
}

public sealed partial class ConjureActionEvent : InstantActionEvent;
