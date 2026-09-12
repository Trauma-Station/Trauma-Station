// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Knowledge component to gain XP whenever you cook food in a microwave etc.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ExperienceOnCookingSystem))]
[AutoGenerateComponentState]
public sealed partial class ExperienceOnCookingComponent : Component
{
    /// <summary>
    /// How much XP you get per food cooked.
    /// </summary>
    [DataField]
    public int Scale = 15;

    /// <summary>
    /// All recipes that have been made so far.
    /// </summary>
    [DataField]
    public HashSet<string> Cooked = new();

    /// <summary>
    /// Limit on gaining XP from cooking the same thing, synced with <see cref="Cooked"/> count.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Limit;
}
