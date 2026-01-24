using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Executions;

[Serializable, NetSerializable]
public sealed class TakeAmmoGetDamageFromProjectileEvent : EntityEventArgs
{
    public DamageSpecifier Damage;
    public bool Delete;
    public TakeAmmoGetDamageFromProjectileEvent(DamageSpecifier damage, bool delete)
    {
        Damage = damage;
        Delete = delete;

    }
}
