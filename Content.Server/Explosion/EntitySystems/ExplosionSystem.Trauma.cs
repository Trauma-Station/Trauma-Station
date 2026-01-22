using Content.Goobstation.Common.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
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
}
