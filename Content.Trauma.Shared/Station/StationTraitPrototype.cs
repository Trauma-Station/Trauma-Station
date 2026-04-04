// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Trauma.Shared.Station;

/// <summary>
/// A possible station trait to pick before a round starts.
/// Happens before the station map is loaded.
/// </summary>
[Prototype]
public sealed partial class StationTraitPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<StationTraitPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Name of this trait
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Description shown in the station report.
    /// If this is set to null, it will not be shown in the report.
    /// </summary>
    [DataField(required: true)]
    public string? Report;

    [DataField]
    public StationTraitGroup Group = StationTraitGroup.Neutral;

    /// <summary>
    /// Weight for random picking.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Entity effects applied to the station entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Traits this one conflicts with.
    /// Must be specified both ways.
    /// </summary>
    [DataField]
    public List<ProtoId<StationTraitPrototype>> Conflicts = new();

    /// <summary>
    /// Returns true if any of this trait's conflicting traits are present in the argument list.
    /// </summary>
    public bool AnyConflicting(List<ProtoId<StationTraitPrototype>> picked)
    {
        foreach (var trait in Conflicts)
        {
            if (picked.Contains(trait))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Each trait group is picked from independently.
/// </summary>
[Serializable, NetSerializable]
public enum StationTraitGroup : byte
{
    Neutral,
    Positive,
    Negative
}
