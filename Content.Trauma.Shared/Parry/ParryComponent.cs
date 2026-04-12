// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Parry;

/// <summary>
/// If an entity holds an item with this component, it can reflect ranged attacks and parry melee attacks, depending on it's melee skill.
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

    /// <summary>
    /// The amount of shots that can be reflected in a quick succession, at 100 skill.
    /// </summary>
    [DataField]
    public int MaxReflects = 3;

    /// <summary>
    /// The amount of shots that can be reflected in a quick succession, at 100 skill.
    /// Should generally be lower than reflects, because most melee weapons are far slower than most guns.
    /// </summary>
    [DataField]
    public int MaxParries = 3;

    /// <summary>
    /// The minimum required level of skill to be able to reflect anything at all.
    /// </summary>
    [DataField]
    public int ReflectMinSkill = 50;

    /// <summary>
    /// The minimum required level of skill to be able to parry anything at all.
    /// </summary>
    [DataField]
    public int ParryMinSkill = 30;

    /// <summary>
    /// The skill required to parry with this weapon.
    /// </summary>
    [DataField]
    public EntProtoId RequiredSkill = "MeleeKnowledge";

    [DataField]
    public Angle ReflectSpread = Angle.FromDegrees(140);

    [DataField]
    public SoundSpecifier? SoundOnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg", AudioParams.Default.WithVariation(0.05f));

    [DataField]
    public SoundSpecifier? SoundOnParry = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg", AudioParams.Default.WithVariation(0.05f));
}
