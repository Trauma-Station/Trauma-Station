// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Prototypes;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Picks all body parts of a given body prototype.
/// </summary>
public sealed partial class BodyPartsSelector : EntityTableSelector
{
    [DataField(required: true)]
    public ProtoId<BodyPrototype> Proto;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        foreach (var slot in proto.Index(Proto).Slots.Values)
        {
            if (slot.Part is {} part)
                yield return part;
        }
    }
}
