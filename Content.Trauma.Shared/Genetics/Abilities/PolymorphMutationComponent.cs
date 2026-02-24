// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Polymorph;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Polymorphs the target into a new body.
/// If the target's current entity prototype is the same as the polymorphed one, it does nothing.
/// Reverts it if the mutation is removed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PolymorphMutationComponent : Component
{
    /// <summary>
    /// The polymorph prototype to use for each allowd species.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<SpeciesPrototype>, ProtoId<PolymorphPrototype>> Prototypes;

    /// <summary>
    /// If non-null and <see cref="Worked"/> is false, will polymorph into this if removed for the current species.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, ProtoId<PolymorphPrototype>> Reverts = new();

    /// <summary>
    /// Fallback for <see cref="Reverts"/> if there is no matching species.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype>? Fallback;

    /// <summary>
    /// If true, will try to revert if the mutation was removed.
    /// </summary>
    [DataField]
    public bool Worked;
}
