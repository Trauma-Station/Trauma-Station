// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Knowledge component that makes it easier to land items in disposal units, according to a skill curve.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ThrowingKnowledgeSystem))]
public sealed partial class ThrowInsertKnowledgeComponent : Component
{
    /// <summary>
    /// The skill curve to use, gets added to chance so output should start at 0 for level 0.
    /// Once it reaches 75% you are guaranteed to land it
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;
}
