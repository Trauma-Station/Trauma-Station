// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Buckle.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;

namespace Content.Trauma.Shared.Construction.Conditions;

/// <summary>
/// Requires that at least 1 entity is strapped to the construction entity.
/// </summary>
[DataDefinition]
public sealed partial class HasStrappedEntity : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<StrapComponent>(uid, out var strap))
            return false;

        return strap.BuckledEntities.Count > 0;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entity = args.Examined;

        var entMan = IoCManager.Resolve<IEntityManager>();
        if (!entMan.TryGetComponent<StrapComponent>(entity, out var strap))
            return false;

        if (strap.BuckledEntities.Count > 0)
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-condition-strap-entity", ("strap", entity)) + "\n");
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        // no guide it's common sense
        yield return new ConstructionGuideEntry();
    }
}
