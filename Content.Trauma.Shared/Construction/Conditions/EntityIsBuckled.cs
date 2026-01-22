// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Buckle.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;

namespace Content.Trauma.Shared.Construction.Conditions;

/// <summary>
/// Requires that the entity is strapped to something.
/// </summary>
[DataDefinition]
public sealed partial class EntityIsBuckled : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<BuckleComponent>(uid, out var strap))
            return false;

        return strap.Buckled;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entity = args.Examined;

        var entMan = IoCManager.Resolve<IEntityManager>();
        if (!entMan.TryGetComponent<StrapComponent>(entity, out var strap))
            return false;

        if (strap.BuckledEntities.Count > 0)
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-condition-buckle-entity", ("entity", entity)) + "\n");
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry();
    }
}
