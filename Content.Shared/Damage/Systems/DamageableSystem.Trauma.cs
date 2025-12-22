using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Systems;

/// <summary>
/// Trauma - API extension for vital damage.
/// </summary>
public sealed partial class DamageableSystem
{
    private static readonly ProtoId<DamageGroupPrototype>[] _vitalOnlyDamageGroups = { "Airloss", "Toxin", "Genetic", "Metaphysical" };
    private readonly List<ProtoId<DamageTypePrototype>> _vitalOnlyDamageTypes = new();

    private void InitializeTrauma()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        CacheVitalPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<DamageGroupPrototype>())
            return;

        CacheVitalPrototypes();
    }

    private void CacheVitalPrototypes()
    {
        _vitalOnlyDamageTypes.Clear();
        foreach (var groupId in _vitalOnlyDamageGroups)
        {
            var group = _prototypeManager.Index(groupId);
            foreach (var type in group.DamageTypes)
            {
                _vitalOnlyDamageTypes.Add(type);
            }
        }
    }

    /// <summary>
    /// Return a new damage specifier which only contains vital-allowed damage types.
    /// </summary>
    public DamageSpecifier GetVitalDamage(DamageSpecifier damage)
    {
        var vitalDamage = new DamageSpecifier();
        foreach (var type in _vitalOnlyDamageTypes)
        {
            if (damage.DamageDict.TryGetValue(type, out var amount))
                vitalDamage.DamageDict[type] = amount;
        }
        return vitalDamage;
    }
}
