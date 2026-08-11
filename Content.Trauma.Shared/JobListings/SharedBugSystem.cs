// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Trauma.Shared.Areas;
using System.Diagnostics.CodeAnalysis;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System facilitating planting bugs in head of staff offices for traitor objectives.
/// The list of areas which bugs have been planted into is stored in the traitor's mind inside <see cref="BugMindArchiveComponent"/>.
/// </summary>
public abstract partial class SharedBugSystem : EntitySystem
{
    [Dependency] protected AreaSystem Area = default!;

    /// <summmary>
    /// Works out if the bug is in the correct area.
    /// </summary>
    public bool IsInCorrectArea(Entity<BugComponent> ent)
    {
        return Area.GetAreaPrototype(ent.Owner) == ent.Comp.TargetArea;
    }

    /// <summmary>
    /// Returns the name of the bug's target area.
    /// </summary>
    protected bool GetAreaName(EntProtoId area, [NotNullWhen(true)] out string? name)
    {
        name = null;
        if (!ProtoMan.Resolve(area, out var prototype))
            return false;

        name = prototype.Name;
        return true;
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<BugComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("bug-examine-target-area", ("target-area", ProtoMan.Index(ent.Comp.TargetArea).Name)));

        if (Transform(ent.Owner).Anchored)
        {
            args.PushMarkup(Loc.GetString(IsInCorrectArea(ent) ? "bug-examine-correct-area" : "bug-examine-incorrect-area"));
        }
    }
}
