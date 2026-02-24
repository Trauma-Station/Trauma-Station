// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Required component for mutation entities that can be added to <see cref="MutatableComponent"/> mobs.
/// Mutation entities must use a prototype, the ID is used for storing and lookups.
/// They must also define a loc string of {ID}-mutated shown to the user when it gets mutated.
/// They can optionally define loc string {ID}-removed shown to the user if it gets removed.
/// <c>UnremovableComponent</c> can be added to prevent removing the mutation.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MutationSystem))]
[AutoGenerateComponentState]
[EntityCategory("Mutations")]
public sealed partial class MutationComponent : Component
{
    /// <summary>
    /// Instability added to the mutated entity by this mutation.
    /// </summary>
    [DataField(required: true)]
    public int Instability;

    /// <summary>
    /// How many bases will be missing when trying to sequence it for the first time.
    /// The chance to roll entire missing pairs increases linearly with this.
    /// Do not increase it past <see cref="MutationData.BaseCount"/> (32).
    /// </summary>
    [DataField]
    public int Difficulty = 8;

    /// <summary>
    /// The target mob this mutation is from.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// Locked mutations cannot be rolled normally, only added through code.
    /// </summary>
    [DataField]
    public bool Locked;

    /// <summary>
    /// These mutations are required by this one.
    /// It cannot be added if any of them are missing.
    /// </summary>
    [DataField]
    public List<EntProtoId<MutationComponent>> Required = new();

    /// <summary>
    /// This mutation cannot be added if any of these are present.
    /// Only works one-way, in most cases you should mirror them.
    /// </summary>
    [DataField]
    public List<EntProtoId<MutationComponent>> Conflicts = new();
}

/// <summary>
/// Event raised on both mutation and target entities after a mutation has been added to a target.
/// </summary>
[ByRefEvent]
public record struct MutationAddedEvent(Entity<MutatableComponent> Target, Entity<MutationComponent> Mutation, EntProtoId<MutationComponent> Id, EntityUid? User, bool Automatic, bool Predicted);

/// <summary>
/// Event raised on both mutation and target entities before a mutation has been removed from a target.
/// </summary>
[ByRefEvent]
public record struct MutationRemovedEvent(Entity<MutatableComponent> Target, Entity<MutationComponent> Mutation, EntProtoId<MutationComponent> Id, EntityUid? User, bool Automatic, bool Predicted);
