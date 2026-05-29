// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Damage.Systems;
using System.Linq;

namespace Content.Trauma.Shared.Damage;

/// <summary>
/// Trauma - moved here from Content.Server
/// </summary>
public sealed partial class DamageOnHitSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnHitComponent, MeleeHitEvent>(DamageItem);
    }
    // Looks for a hit, then damages the held item an appropriate amount.
    private void DamageItem(EntityUid uid, DamageOnHitComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Any())
            _damage.TryChangeDamage(uid, component.Damage, component.IgnoreResistances, targetPart: component.TargetParts); // Goob - added targetPart
    }
}
