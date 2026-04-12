// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;

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
}
