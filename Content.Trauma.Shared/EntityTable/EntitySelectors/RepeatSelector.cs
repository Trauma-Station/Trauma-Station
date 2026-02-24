// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Trauma.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Repeats the result of an entity selector <see cref="Count"/> times.
/// Useful with GroupSelector to avoid repeating amount: N for every item
/// </summary>
public sealed partial class RepeatSelector : EntityTableSelector
{
    [DataField(required: true)]
    public EntityTableSelector Repeated = default!;

    [DataField(required: true)]
    public int Count;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        var picked = Repeated.GetSpawns(rand, entMan, proto, ctx).ToList();
        for (int i = 0; i < Count; i++)
        {
            foreach (var id in picked)
            {
                yield return id;
            }
        }
    }
}
