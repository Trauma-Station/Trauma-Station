// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
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
    /// Chance of this trait being in the group's pool at all.
    /// Similar to a weight but simpler to implement.
    /// </summary>
    [DataField]
    public float Chance = 1f;

    /// <summary>
    /// Minimum playercount to allow picking this trait.
    /// </summary>
    [DataField]
    public int MinPlayers = 0;

    /// <summary>
    /// Entity effects applied to the station entity immediately.
    /// </summary>
    [DataField]
    public EntityEffect[]? Effects;

    /// <summary>
    /// Entity effects applied to the station entity after the map has been loaded, but before it has been initialized.
    /// </summary>
    [DataField]
    public EntityEffect[]? StartEffects;

    /// <summary>
    /// Entity effects applied to the station entity after the map has been initialized, but before players have spawned.
    /// </summary>
    [DataField]
    public EntityEffect[]? MapEffects;

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
