// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Quality;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Construction.Prototypes;

public sealed partial class ConstructionPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ConstructionPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Knowledge masteries that are required to be able to make this craft.
    /// Mastery is from 0-5.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Theory = new();

    /// <summary>
    /// If non-null, the skills you need to make a normal quality item.
    /// This lets you make something easy to understand how to make but hard to do well.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, int>? Practical;

    /// <summary>
    /// Optional quality override.
    /// </summary>
    [DataField]
    public ProtoId<QualityPrototype>? QualityPrototype;

    /// <summary>
    /// Whether to give the resulting item a quality at all.
    /// </summary>
    [DataField]
    public bool UseQuality = true;
}
