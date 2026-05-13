// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Goobstation.Shared.Wraith.Other;

public sealed partial class DamageOnCollideSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable  = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<DamageOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (!_whitelist.CheckBoth(args.OtherEntity, ent.Comp.IgnoreWhitelist, ent.Comp.Whitelist))
            return;
        var target = ent.Comp.Inverted ? args.OtherEntity : ent.Owner;
        _damageable.TryChangeDamage(target, ent.Comp.Damage);
    }
}
