// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.SlaughterDemon;

/// <summary>
/// This is used for marking an entity as able to devour people with blood crawl
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlaughterDevourComponent : Component
{
    /// <summary>
    /// Devouring doafter
    /// </summary>
    [DataField(required: true)]
    public float DoAfterDelay;

    /// <summary>
    /// Base damage done to the demon after devouring osmeone, scaled by the healing amount.
    /// Must be negative to heal.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier HealDamage = default!;

    /// <summary>
    /// Healing done when eating someone.
    /// </summary>
    [DataField]
    public FixedPoint2 ToHeal = 100;

    /// <summary>
    /// Healing done when eating a robot
    /// </summary>
    [DataField]
    public FixedPoint2 ToHealNonCrew = 50;

    /// <summary>
    /// Healing done when eating anything else
    /// </summary>
    [DataField]
    public FixedPoint2 ToHealAnythingElse = 25;

    /// <summary>
    /// The sound that plays once devouring someone
    /// </summary>
    [DataField]
    public SoundSpecifier? FeastSound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// A container that holds the entities instead of outright removing them
    /// </summary>
    [DataField]
    public Container? Container;
}
