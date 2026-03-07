using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Reflect;

/// <summary>
/// If an entity holds an item with this component, it has a chance to reflect ranged attacks depending on it's melee skill.
/// Uses <c>ItemToggleComponent</c> to control reflection.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SkillBasedReflectComponent : Component
{
    /// <summary>
    /// What we reflect.
    /// </summary>
    [DataField]
    public ReflectType Reflects = ReflectType.Energy | ReflectType.NonEnergy;

    /// <summary>
    /// The base chance to reflect an attack at around 70 melee skill and 0 exhaustion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseProb = 1f;

    /// <summary>
    /// How much exhaustion is added each time the user tries to reflect an attack.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ExhaustionPerReflectAttempt = 0.1f;

    /// <summary>
    /// The skill required to reflect with this weapon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId RequiredSkill = "MeleeKnowledge";

    /// <summary>
    /// The minimum required level of skill to be able to reflect anything at all.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinSkill = 40;

    /// <summary>
    /// Probability for a projectile to be reflected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle Spread = Angle.FromDegrees(45);

    /// <summary>
    /// The sound to play when reflecting.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundOnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg", AudioParams.Default.WithVariation(0.05f));
}
