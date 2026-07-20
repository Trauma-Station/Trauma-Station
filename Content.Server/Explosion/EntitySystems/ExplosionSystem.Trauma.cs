// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Explosion;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery = default!;

    private float PartVariation;
    private float WoundMultiplier;

    private ProtoId<DamageTypePrototype>[] _types = { "Blunt", "Slash", "Piercing", "Heat", "Cold" };

    private void SubscribeTrauma()
    {
        Subs.CVar(_cfg, GoobCVars.ExplosionLimbDamageVariation, x => PartVariation = x, true);
        Subs.CVar(_cfg, GoobCVars.ExplosionWoundMultiplier, x => WoundMultiplier = x, true);
    }

    private void ModifyWoundSeverities(DamageSpecifier damage)
    {
        if (damage.PartDamageVariation == 0f)
            damage.PartDamageVariation = PartVariation;
        foreach (var type in _types)
        {
            damage.WoundSeverityMultipliers.TryAdd(type, WoundMultiplier);
        }
    }

    private void DamageBody(Entity<BodyComponent> ent, DamageSpecifier damage, string explosion)
    {
        foreach (var part in _body.GetExternalOrgans(ent.AsNullable()))
        {
            var resistanceEv = new GetExplosionResistanceEvent(explosion);
            RaiseLocalEvent(part, ref resistanceEv);
            resistanceEv.DamageCoefficient = Math.Max(0, resistanceEv.DamageCoefficient);

            var partDamage = resistanceEv.DamageCoefficient != 1
                ? damage * resistanceEv.DamageCoefficient
                : damage;

            _damageableSystem.ChangeDamage(part.Owner, partDamage, ignoreResistances: true, ignoreGlobalModifiers: true);
        }
    }
}
