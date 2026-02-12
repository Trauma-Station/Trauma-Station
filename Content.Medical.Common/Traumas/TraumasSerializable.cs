using Robust.Shared.Serialization;

namespace Content.Medical.Common.Traumas;

[Serializable, NetSerializable]
public enum TraumaType
{
    BoneDamage,
    OrganDamage,
    VeinsDamage,
    NerveDamage, // pain
    Dismemberment,
}

[Serializable, NetSerializable]
public enum OrganSeverity
{
    Normal = 0,
    Damaged = 1,
    Destroyed = 2, // obliterated
}

[Serializable, NetSerializable]
public enum BoneSeverity
{
    Normal = 0,
    Damaged = 1,
    Cracked = 2,
    Broken = 3, // Ha-ha.
}
