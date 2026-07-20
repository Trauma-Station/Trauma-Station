// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Trauma.Shared.Forging;

namespace Content.Trauma.Shared.Construction.Conditions;

/// <summary>
/// Requires that a metallic entity has cooled down below its workable range.
/// </summary>
public sealed partial class QuenchMetal : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entMan)
    {
        var metal = entMan.System<SharedMetalSystem>();
        return !metal.IsWorkable(uid);
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entity = args.Examined;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var metal = entMan.System<SharedMetalSystem>();
        if (!metal.IsWorkable(entity))
            return false;

        args.PushMarkup("Quench it first!\n");
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        // no guide it goes without saying
        yield return new ConstructionGuideEntry();
    }
}
