// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Silicon.Charge;

[RegisterComponent]
public sealed partial class BatteryDrinkerSourceComponent : Component
{
    /// <summary>
    ///     The max amount of power this source can provide in one sip.
    ///     No limit if zero.
    /// </summary>
    [DataField]
    public float MaxAmount;

    /// <summary>
    ///     The multiplier for the drink speed.
    /// </summary>
    [DataField]
    public float DrinkSpeedMulti = 1f;

    /// <summary>
    ///     The sound to play when the battery gets drunk from.
    /// </summary>
    [DataField]
    public SoundSpecifier? DrinkSound = new SoundCollectionSpecifier("sparks");
}
