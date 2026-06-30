// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System facilitating planting bugs in head of staff offices for traitor objectives.
/// The list of areas which bugs have been planted into is stored in the traitor's mind inside <see cref="BugMindArchiveComponent"/>.
/// </summary>
public abstract partial class SharedBugSystem : EntitySystem
{
    [Dependency] protected IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BugComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<BugComponent> entity, ref ExaminedEvent args)
    {
        if (!_proto.Resolve(entity.Comp.TargetArea, out var prototype))
            return;

        args.PushMarkup(Loc.GetString("bug-examine-target-area", ("target-area", prototype.Name)));
    }
}
