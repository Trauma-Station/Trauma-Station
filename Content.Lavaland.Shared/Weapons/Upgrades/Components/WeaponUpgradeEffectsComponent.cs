using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Lavaland.Shared.Weapons.Upgrades.Components;

/// <summary>
/// Applies a list of entity effects on a target when hit with a melee attack.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeaponUpgradeEffectsComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
