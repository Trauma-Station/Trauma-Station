// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Goobstation.Common.Singularity;
using Content.Shared.Examine;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Lock;

public sealed class LabyrinthHandbookSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LabyrinthHandbookComponent, BeforeHolosignUsedEvent>(OnBeforeHolosign);

        SubscribeLocalEvent<ContainmentFieldThrowEvent>(OnThrow);
    }

    private void OnThrow(ref ContainmentFieldThrowEvent args)
    {
        if (!HasComp<LabyrinthWallComponent>(args.Field))
            return;

        if (_heretic.IsHereticOrGhoul(args.Entity))
        {
            args.Cancelled = true;
            return;
        }

        var ev = new BeforeCastTouchSpellEvent(args.Entity, false);
        RaiseLocalEvent(args.Entity, ev, true);
        args.Cancelled = ev.Cancelled;
    }

    private void OnBeforeHolosign(Entity<LabyrinthHandbookComponent> ent, ref BeforeHolosignUsedEvent args)
    {
        args.Handled = true;

        if (!_heretic.IsHereticOrGhoul(args.User) || !_examine.InRangeUnOccluded(args.User, args.ClickLocation))
            args.Cancelled = true;
    }
}
