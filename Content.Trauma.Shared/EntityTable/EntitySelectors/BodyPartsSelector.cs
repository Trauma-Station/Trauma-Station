// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Shared.Body;
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
    public EntProtoId<InitialBodyComponent> Proto;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        var ent = proto.Index(Proto);
        var factory = entMan.ComponentFactory;
        if (!ent.TryGetComponent<InitialBodyComponent>(out var body, factory))
            yield break; // unreachable

        foreach (var organId in body.Organs.Values)
        {
            // filter out internal organs from the fill
            var organ = proto.Index(organId);
            // TODO: change this to .HasComp after engine update
            if (!organ.TryGetComponent<InternalOrganComponent>(out _, factory))
                yield return organId;
        }
    }
}
