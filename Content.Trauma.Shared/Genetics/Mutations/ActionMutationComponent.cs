// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Grants the mutation user an action.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ActionMutationSystem))]
public sealed partial class ActionMutationComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<ActionComponent> Action;

    [DataField]
    public EntityUid? ActionEntity;
}
