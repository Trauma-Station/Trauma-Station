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
    /// The polymorph prototype to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype;

    /// <summary>
    /// If non-null and <see cref="Worked"/> is false, will polymorph into this if removed.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype>? Fallback;

    /// <summary>
    /// If true, will try to revert if the mutation was removed.
    /// </summary>
    [DataField]
    public bool Worked;
}
