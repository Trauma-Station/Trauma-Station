// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Trauma.Shared.Areas;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System facilitating planting bugs in head of staff offices for traitor objectives.
/// The list of areas which bugs have been planted into is stored in the traitor's mind inside <see cref="BugMindArchiveComponent"/>.
/// </summary>
public abstract partial class SharedBugSystem : EntitySystem
{
    [Dependency] protected IPrototypeManager _proto = default!;
    [Dependency] protected AreaSystem _area =  default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BugComponent, ExaminedEvent>(OnExamine);
    }

    /// <summmary>
    /// Works out if the bug is in the correct area.
    /// </summary>
    public bool IsInCorrectArea(Entity<BugComponent> entity)
    {
        var area = _area.GetArea(entity.Owner);
        if (area is null)
            return false;
        var prototype = MetaData(area.Value).EntityPrototype;
        if (prototype is null)
            return false;
        return prototype.ID == entity.Comp.TargetArea;
    }

    private void OnExamine(Entity<BugComponent> entity, ref ExaminedEvent args)
    {
        if (!_proto.Resolve(entity.Comp.TargetArea, out var prototype))
            return;

        args.PushMarkup(Loc.GetString("bug-examine-target-area", ("target-area", prototype.Name)));

        if (Transform(entity.Owner).Anchored)
        {
            args.PushMarkup(Loc.GetString(IsInCorrectArea(entity) ? "bug-examine-correct-area" : "bug-examine-incorrect-area"));
        }
    }
}
