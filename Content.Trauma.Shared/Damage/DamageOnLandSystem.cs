// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Damage.Systems;
using Content.Shared.Throwing;
using Content.Trauma.Common.Mail;

namespace Content.Trauma.Shared.Damage;

/// <summary>
/// Trauma - moved this out of server
/// Damages the thrown item when it lands.
/// </summary>
public sealed class DamageOnLandSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnLandComponent, LandEvent>(DamageOnLand);
        SubscribeLocalEvent<DamageOnLandComponent, MailFragileEvent>(OnMailFragile);
    }

    private void DamageOnLand(Entity<DamageOnLandComponent> ent, ref LandEvent args)
    {
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, ignoreResistances: ent.Comp.IgnoreResistances);
    }

    // TODO MAIL: remove all mail shitcode from this file
    private void OnMailFragile(Entity<DamageOnLandComponent> ent, ref MailFragileEvent args)
    {
        args.Fragile = true;
    }
}
