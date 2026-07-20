// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Knowledge component to gain XP whenever you cook food in a microwave etc.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ExperienceOnCookingSystem))]
public sealed partial class ExperienceOnCookingComponent : Component
{
    /// <summary>
    /// How much XP you get per food cooked.
    /// </summary>
    [DataField]
    public int Scale = 1;
}
