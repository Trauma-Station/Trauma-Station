// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Prototypes;

namespace Content.Trauma.Shared.Body.Part;

/// <summary>
/// Adds innate tackle (dash) for moths and a bonus modifier to tackle for either moth or just anyone
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DashWingsComponent : Component
{
    /// <summary>
    /// List of species that it works for.
    /// Human brain can't use moth wings effectively.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>>? SpeciesWhitelist;

    [DataField, AutoNetworkedField]
    public bool Changed;

    [DataField]
    public float SkillMod = 2f;

    [DataField]
    public float RangeModifier = 1f;

    [DataField(required: true)]
    public ComponentRegistry ToAdd = default!;
}
