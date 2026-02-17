// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Allows an entity to have mutations.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MutationSystem))]
[AutoGenerateComponentState]
public sealed partial class MutatableComponent : Component
{
    /// <summary>
    /// The name of the container that stores mutations.
    /// </summary>
    [DataField]
    public string ContainerId = "mutations";

    /// <summary>
    /// Currently active mutations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<MutationComponent>, EntityUid> Mutations = new();

    /// <summary>
    /// Dormant mutations that can be added with a Activator for no instability cost.
    /// They are also what go into a mob's sequenced mutations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId<MutationComponent>> Dormant = new();

    /// <summary>
    /// Maximum number of dormant mutations to pick on map init.
    /// </summary>
    [DataField]
    public int MaxDormant = 8;

    /// <summary>
    /// Add these mutations on map init, with a chance from 0-1.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId<MutationComponent>, float> DefaultMutations = new();

    /// <summary>
    /// How much instability you have from mutations.
    /// Once this reaches <see cref="MaxInstability"/> it's joever.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TotalInstability;

    /// <summary>
    /// Max instability where your DNA starts melting.
    /// </summary>
    [DataField]
    public int MaxInstability = 100;

    /// <summary>
    /// Status effect added while DNA is melting.
    /// </summary>
    [DataField]
    public EntProtoId<StatusEffectComponent> MeltingEffect = "DnaMeltingStatusEffect";

    /// <summary>
    /// How long <see cref="MeltingEffect"/> lasts for.
    /// </summary>
    [DataField]
    public TimeSpan MeltDuration = TimeSpan.FromMinutes(3); // its 10m in tg but that seems like way too long
}
