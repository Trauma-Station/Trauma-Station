// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Goobstation.Shared.Devil.Contract;

[Prototype("clause")]
public sealed partial class DevilClausePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public int ClauseWeight;

    [DataField]
    public ComponentRegistry? AddedComponents;

    [DataField]
    public ComponentRegistry? RemovedComponents;

    [DataField]
    public string? DamageModifierSet;

    // TODO: kill
    [DataField]
    public BaseDevilContractEvent? Event;

    [DataField]
    public EntityEffect[]? Effects;

    [DataField]
    public List<EntProtoId>? Implants;

    [DataField]
    public List<EntProtoId>? SpawnedItems;
}
