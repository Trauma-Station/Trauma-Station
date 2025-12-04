// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Runs entity effects on the mutation target when this mutation is triggered.
/// The mob state can also be filtered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EffectOnTriggerMutationComponent : Component
{
    /// <summary>
    /// The effects to run on the target.
    /// </summary>
    [DataField(required: true)]
    public List<EntityEffect> Effects = new();
}
