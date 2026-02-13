// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.CardboardBox;

/// <summary>
/// Disables a box's stealth when opened near witnesses (people outside of it)
/// It will re-enable after <see cref="StealthCooldown"/>.
/// Huh?
/// </summary>
/// <remarks>
/// Just a box...
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(JustABoxSystem))]
public sealed partial class JustABoxComponent : Component
{
    /// <summary>
    /// How long it gets put on cooldown after being opened near witnesses.
    /// </summary>
    [DataField]
    public TimeSpan StealthCooldown = TimeSpan.FromSeconds(40);

    /// <summary>
    /// Range to look for witnesses in.
    /// </summary>
    [DataField]
    public float WitnessRange = 6f;
}
