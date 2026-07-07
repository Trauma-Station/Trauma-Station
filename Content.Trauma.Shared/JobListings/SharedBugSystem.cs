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

    /// <summmary>
    /// Returns the name of the bug's target area.
    /// </summary>
    protected bool GetAreaName(EntProtoId area, [NotNullWhen(true)] out string? name)
    {
        name = null;
        if (!_proto.Resolve(area, out var prototype))
            return false;

        name = prototype.Name;
        return true;
    }

    private void OnExamine(Entity<BugComponent> entity, ref ExaminedEvent args)
    {
        if (!GetAreaName(entity.Comp.TargetArea, out var name))
            return;

        args.PushMarkup(Loc.GetString("bug-examine-target-area", ("target-area", name)));

        if (Transform(entity.Owner).Anchored)
        {
            args.PushMarkup(Loc.GetString(IsInCorrectArea(entity) ? "bug-examine-correct-area" : "bug-examine-incorrect-area"));
        }
    }
}
