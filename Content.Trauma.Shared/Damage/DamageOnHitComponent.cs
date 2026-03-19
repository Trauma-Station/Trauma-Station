// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Robust.Shared.GameStates;
// </Trauma>
using Content.Shared.Damage;

// Damages the entity by a set amount when it hits someone.
// Can be used to make melee items limited-use or make an entity deal self-damage with unarmed attacks.
namespace Content.Trauma.Shared.Damage;

/// <summary>
/// Trauma - moved to shared and networked this slop
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DamageOnHitComponent : Component
{
    [DataField]
    public bool IgnoreResistances = true;

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Goob - The body parts to deal damage to.
    /// When there is more than one listed element,
    /// randomly selects between one of the elements.
    /// </summary>
    [DataField]
    public TargetBodyPart? TargetParts;
}
