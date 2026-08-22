// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Stacks;

namespace Content.Trauma.Shared.Construction.Conditions;

/// <summary>
/// Requires that a stack entity has a certain number of items.
/// </summary>
public sealed partial class StackCountCondition : IGraphCondition
{
    [DataField(required: true)]
    public int Count;

    public bool Condition(EntityUid uid, IEntityManager entMan)
        => entMan.System<SharedStackSystem>().GetCount(uid) == Count;

    public bool DoExamine(ExaminedEvent args)
    {
        var entity = args.Examined;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var stackSys = entMan.System<SharedStackSystem>();
        var count = stackSys.GetCount(entity);
        if (count == Count)
            return false;

        var diff = Math.Abs(count - Count);
        var word = diff == 1 ? "piece" : "pieces";
        args.PushMarkup(count < Count
            ? $"You need {diff} more {word}\n"
            : $"You need to remove {diff} {word} from it\n");
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-guide-condition-stack-count",
            Arguments = new (string, object)[]
            {
                ("count", Count)
            }
        };
    }
}
