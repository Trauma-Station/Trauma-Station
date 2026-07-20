// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Power.Components;

[RegisterComponent]
public sealed partial class BatteryDrinkerComponent : Component
{
    /// <summary>
    ///     How long it takes to drink from a battery, in seconds.
    ///     Is multiplied by the source.
    /// </summary>
    [DataField]
    public float DrinkSpeed = 1.5f;

    /// <summary>
    ///     The multiplier for the amount of power to attempt to drink.
    ///     Default amount is 1000
    /// </summary>
    [DataField]
    public float DrinkMultiplier = 5f;

    /// <summary>
    ///     Blacklist for battery containers that can not be drank from.
    /// </summary>
    /// <remarks>
    ///     This should not be used to disable drinking from a type of power cell, as it is not checked for entities
    ///     inside a power cell slot. If you want to ban drinking from a power cell, remove BatteryDrinkerSourceComponent
    ///     from it.
    /// </remarks>
    [DataField]
    public EntityWhitelist? Blacklist;
}
