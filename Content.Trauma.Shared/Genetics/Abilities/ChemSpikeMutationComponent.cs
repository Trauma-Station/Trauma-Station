// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Mutation component for chemspike.
/// Stores the transfer chemicals action.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ChemSpikeMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class ChemSpikeMutationComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> Action;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// The mob to transfer chemicals to.
    /// </summary>
    [DataField]
    public EntityUid? Target;

    /// <summary>
    /// The chemspike entity that was shot.
    /// </summary>
    [DataField]
    public EntityUid? Projectile;

    /// <summary>
    /// Limit on how many chemicals to flush from the user's bloodstream.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxQuantity = 100;
}
