// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Genetics.Tools;

/// <summary>
/// Injector that tries to mutate whoever it injects.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MutatorSystem))]
[AutoGenerateComponentState]
public sealed partial class MutatorComponent : Component
{
    /// <summary>
    /// The set of mutations to try add to a target.
    /// If this is empty the mutator is spent and cannot be reused.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId<MutationComponent>> Mutations = new();

    /// <summary>
    /// Uses ActivateMutation instead of AddMutation, so it'll only do anything for Dormant mutations.
    /// </summary>
    [DataField]
    public bool Activator;

    // TODO: make this a component or something not this
    /// <summary>
    /// Removes mutations instead of adding them.
    /// <see cref="Activator"/> is then ignored.
    /// </summary>
    [DataField]
    public bool Remove;

    /// <summary>
    /// How long it takes to inject the mutator into yourself.
    /// Doubled when used on others.
    /// </summary>
    [DataField]
    public TimeSpan InjectTime = TimeSpan.FromSeconds(3);
}

[Serializable, NetSerializable]
public enum MutatorVisuals : byte
{
    Layer,
    Spent
}
