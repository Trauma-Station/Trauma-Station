// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Construction.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Content.Trauma.Common.Quality;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// A item that can be forged, either a complete ready item or a part for crafting with.
/// Starts off as unfinished X which can be wrought into the result item/part.
/// </summary>
[Prototype]
public sealed partial class ForgedItemPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ForgedItemPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    /// The category this item belongs to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ForgingCategoryPrototype> Category;

    /// <summary>
    /// Non-procedurally generated item that just has its metal and sprites etc set
    /// </summary>
    [DataField]
    public EntProtoId? Result;

    /// <summary>
    /// Name to give procedurally generated items.
    /// Must be set if result is null.
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>
    /// Get the displayed name for an item.
    /// </summary>
    public string DisplayName(IPrototypeManager protoMan)
        => Result is {} id ? protoMan.Index(id).Name : Name;

    /// <summary>
    /// Tag to give the finished procgen item after it has cooled down.
    /// Use this in construction graphs where this is not the starting point.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype>? Tag;

    /// <summary>
    /// Construction graph to give the wrought procgen item.
    /// If this is defined <see cref="Result"/> must also be defined.
    /// </summary>
    [DataField]
    public ProtoId<ConstructionGraphPrototype>? Construction;

    /// <summary>
    /// The finished item to spawn when finishing <see cref="Construction"/>.
    /// </summary>
    [DataField]
    public EntProtoId? Finished;

    /// <summary>
    /// Base amount of blunt damage needed to work the unfinished item into the result.
    /// </summary>
    [DataField]
    public FixedPoint2 Work = 100;

    /// <summary>
    /// How many items are created after working the unfinished item.
    /// </summary>
    [DataField]
    public int Amount = 1;

    /// <summary>
    /// How many ingots it takes to make this item.
    /// </summary>
    [DataField]
    public int Cost = 1;

    /// <summary>
    /// RSI path to use for procedurally generated item sprites.
    /// </summary>
    [DataField]
    public ResPath? Sprite;

    /// <summary>
    /// Quality modifier overrides to use for the resulting item.
    /// </summary>
    [DataField]
    public ProtoId<QualityPrototype>? QualityPrototype;

    /// <summary>
    /// Base skill mastery needed to produce average quality items.
    /// Can be raised by the metal used.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, int> Skills = new()
    {
        { "MetalworkingKnowledge", 1 }
    };

    /// <summary>
    /// If non-null, only these metals can be used to forge this item.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MetalPrototype>>? Whitelist;

    /// <summary>
    /// If non-null, these metals cannot be used to forge this item.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MetalPrototype>>? Blacklist;
}
