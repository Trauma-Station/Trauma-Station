using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Executions;

[Serializable, NetSerializable]
public sealed class TakeAmmoGetDamageFromProjectileEvent : EntityEventArgs
{
    public DamageSpecifier Damage;
    public TakeAmmoGetDamageFromProjectileEvent(DamageSpecifier damage)
    {
        Damage = damage;
    }
}
