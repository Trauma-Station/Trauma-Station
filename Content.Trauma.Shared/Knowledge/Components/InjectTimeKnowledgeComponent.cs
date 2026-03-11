// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Knowledge component to modify injector time according to a skill curve.
/// For instant injections (hyposprays) it will instead give it a delay at low skill.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(FirstAidKnowledgeSystem))]
public sealed partial class InjectTimeKnowledgeComponent : Component
{
    /// <summary>
    /// The skill curve to use.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;

    /// <summary>
    /// Minimum skill to have hypospray injections be instant.
    /// </summary>
    [DataField]
    public int MinSkill = 20;

    /// <summary>
    /// How long instant injections must take if below <see cref="MinSkill"/>.
    /// Not affected by the curve.
    /// </summary>
    [DataField]
    public TimeSpan MinTime = TimeSpan.FromSeconds(1.5);
}
