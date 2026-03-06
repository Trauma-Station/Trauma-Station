// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Knowledge component that makes it easier to land items in disposal units, according to a quadratic curve.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ThrowingKnowledgeSystem))]
public sealed partial class ThrowInsertKnowledgeComponent : Component
{
    /// <summary>
    /// Offset to apply to the skill before the curve.
    /// </summary>
    [DataField]
    public int Offset;

    /// <summary>
    /// How to scale the curve, roughly which level + offset is needed to add 100% chance.
    /// </summary>
    [DataField]
    public float InverseScale = 100f;
}
