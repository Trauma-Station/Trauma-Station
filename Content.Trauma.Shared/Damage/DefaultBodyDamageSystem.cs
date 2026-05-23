// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;

namespace Content.Trauma.Shared.Damage;

public sealed partial class DefaultBodyDamageSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DefaultBodyDamageComponent, BodyInitEvent>(OnBodyInit); // needs the parts to exist to damage them
    }

    private void OnBodyInit(Entity<DefaultBodyDamageComponent> ent, ref BodyInitEvent args)
    {
        var damage = ent.Comp.Damage * _body.GetVitalBodyPartRatio(ent.Owner);
        _damage.ChangeDamage(ent.Owner, damage, ignoreResistances: true, targetPart: TargetBodyPart.All);
    }
}
