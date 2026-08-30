// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using System.Text;

namespace Content.Shared.Damage.Systems;

/// <summary>
/// Trauma - API extension for vital damage.
/// </summary>
public sealed partial class DamageableSystem
{
    [Dependency] private CommonBodyPartSystem _part = default!;
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery = default!;
    [Dependency] private EntityQuery<InorganicComponent> _inorganicQuery = default!;
    [Dependency] private EntityQuery<InternalChildOrganComponent> _internalQuery = default!;

    private static readonly ProtoId<DamageGroupPrototype>[] _vitalOnlyDamageGroups = { "Airloss", "Toxin", "Genetic", "Metaphysical" };
    private readonly List<ProtoId<DamageTypePrototype>> _vitalOnlyDamageTypes = new();

    private StringBuilder _sb = new();

    private void CacheVitalPrototypes()
    {
        _vitalOnlyDamageTypes.Clear();
        foreach (var groupId in _vitalOnlyDamageGroups)
        {
            var group = ProtoMan.Index(groupId);
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

    /// <summary>
    /// Get a pretty string of a damage specifier.
    /// </summary>
    public string ToPrettyString(DamageSpecifier damage)
    {
        _sb.Clear();
        _sb.Append("T ");
        _sb.Append(damage.GetTotal());

        _sb.Append(" { ");
        foreach (var (type, amount) in damage.DamageDict)
        {
            _sb.Append(type.Id);
            _sb.Append('/');
            _sb.Append(amount);
            _sb.Append(' ');
        }
        _sb.Append("}");
        return _sb.ToString();
    }

    /// <summary>
    /// Get a pretty string of an entity's <see cref="GetAllDamage"/>.
    /// </summary>
    public string DumpDamage(Entity<DamageableComponent?> ent)
        => ToPrettyString(GetAllDamage(ent));
}
