// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Parry;

/// <summary>
/// If an entity holds an item with this component, it can reflect ranged attacks and parry melee attacks.
/// Uses <c>ItemToggleComponent</c> to control reflection.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ParryComponent : Component
{
    /// <summary>
    /// What we reflect.
    /// </summary>
    [DataField]
    public ReflectType Reflects = ReflectType.Energy | ReflectType.NonEnergy;

    [DataField]
    public float ParryExhaustionCost = 0.5f;

    [DataField]
    public float ReflectExhaustionCost = 1.1f; // > 1 means can't reflect

    /// <summary>
    /// The minimum required level of skill to be able to reflect anything at all.
    /// </summary>
    [DataField]
    public int ReflectMinSkill = 26;

    /// <summary>
    /// The minimum required level of skill to be able to parry anything at all.
    /// </summary>
    [DataField]
    public int ParryMinSkill = 26;

    [DataField]
    public Angle ReflectSpread = Angle.FromDegrees(140);

    [DataField]
    public SoundSpecifier? SoundOnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg", AudioParams.Default.WithVariation(0.05f));

    [DataField]
    public SoundSpecifier? SoundOnParry = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg", AudioParams.Default.WithVariation(0.05f));
}
