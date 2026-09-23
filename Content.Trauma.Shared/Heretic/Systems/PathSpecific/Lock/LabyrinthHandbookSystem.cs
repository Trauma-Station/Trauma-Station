// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Goobstation.Common.Singularity;
using Content.Shared.Examine;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Lock;

public sealed partial class LabyrinthHandbookSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private EntityQuery<LabyrinthWallComponent> _wallQuery = default!;

    [SubscribeLocalEvent]
    private void OnThrow(ref ContainmentFieldThrowEvent args)
    {
        if (!_wallQuery.HasComp(args.Field))
            return;

        if (_heretic.IsHereticOrGhoul(args.Entity))
        {
            args.Cancelled = true;
            return;
        }

        var ev = new BeforeCastTouchSpellEvent(args.Entity, false);
        RaiseLocalEvent(args.Entity, ref ev, true);
        args.Cancelled = ev.Cancelled;
    }

    [SubscribeLocalEvent]
    private void OnBeforeHolosign(Entity<LabyrinthHandbookComponent> ent, ref BeforeHolosignUsedEvent args)
    {
        args.Handled = true;

        if (!_heretic.IsHereticOrGhoul(args.User) || !_examine.InRangeUnOccluded(args.User, args.ClickLocation))
            args.Cancelled = true;
    }
}
