// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Prototypes;

namespace Content.Trauma.Shared.Ghost;

/// <summary>
/// Antag gamerule equivalent for <see cref="GhostCharacterSpawnerComponent"/>.
/// Doesn't do component stuff, antag defintions can do that.
/// </summary>
[RegisterComponent]
public sealed partial class AntagGhostCharacterRuleComponent : Component
{
    [DataField]
    public ProtoId<SpeciesPrototype> DefaultSpecies = "Human";
}
