// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Melee;

public sealed partial class MeleeWeaponComponent
{
    /// <summary>
    /// If false, attacks by this weapon cannot be parried
    /// </summary>
    [DataField]
    public bool CanBeParried = true;

    /// <summary>
    /// If true, alt attack will use click rather than wide swing
    /// </summary>
    [DataField]
    public bool AltClickAttack;

    /// <summary>
    /// Whitelist for the entities that can be hit on click attacks
    /// </summary>
    [DataField]
    public EntityWhitelist? ClickAttackWhitelist;

    /// <summary>
    /// If <see cref="AltClickAttack"/> is true, this acts as whitelist for entities that can be hit on click alt
    /// attacks
    /// </summary>
    [DataField]
    public EntityWhitelist? AltClickAttackWhitelist;

    /// <summary>
    /// If false, melee weapon won't miss on click attacks
    /// </summary>
    [DataField]
    public bool CanMiss = true;

    /// <summary>
    /// Applies stamina damage on each successful wideswing hit to the attacker.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeavyStaminaCost = 10f;

    [DataField, AutoNetworkedField]
    public EntProtoId MissAnimation = "WeaponArcPunch";

    [DataField, AutoNetworkedField]
    public bool FlipAnimation = true;

    [DataField, AutoNetworkedField]
    public EntProtoId DisarmAnimation = "WeaponArcDisarm";

    /// <summary>
    /// Rotation of the animation.
    /// 0 degrees means the top faces the attacker.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle AnimationRotation = Angle.Zero;

    /// <summary>
    /// Shitmed Change: Part damage is multiplied by this amount for single-target attacks
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ClickPartDamageMultiplier = 1f;

    /// <summary>
    /// Shitmed Change: Part damage is multiplied by this amount for heavy swings
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeavyPartDamageMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public bool CanWideSwing = true;

    [DataField, AutoNetworkedField]
    public float HeavyAttackWoundMultiplier = 0.5f;
}
