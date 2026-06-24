// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Areas;

public sealed partial class AreaDetectorSystem : EntitySystem
{
    [Dependency] private AreaSystem _area = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AreaDetectorComponent, MoveEvent>(OnMove);
    }

    private void OnMove(Entity<AreaDetectorComponent> ent, ref MoveEvent args)
    {
        if (TerminatingOrDeleted(ent) || args.Entity.Comp1.MapUid == null)
            return;

        if (_area.GetArea(ent.Owner) is not { } area || TerminatingOrDeleted(area))
        {
            // If we were in an area before, and we enter a place where there's no areas, then raise an early event.
            if (ent.Comp.Area is { } areaWas)
            {
                var earlyEv = new AreaDetectorChangedEvent(areaWas, null);
                RaiseLocalEvent(ent.Owner, ref earlyEv);
            }

            ent.Comp.Area = null;
            Dirty(ent);
            return;
        }

        // We enter the new area, whilst previous one was null.
        if (ent.Comp.Area is not { } areaIn || TerminatingOrDeleted(areaIn))
        {
            var midEv = new AreaDetectorChangedEvent(null, area);
            RaiseLocalEvent(ent.Owner, ref midEv);

            ent.Comp.Area = area;
            Dirty(ent);
            return;
        }

        // We enter a new area, while being on another area
        if (_area.GetAreaPrototype(areaIn) is not { } areaInProto || _area.GetAreaPrototype(area) is not { } areaToEnterProto)
            return;

        // Don't trigger for every area of the same entity prototype.
        if (areaInProto == areaToEnterProto)
            return;

        // We enter the new area, passing the new area we are going into.
        var ev = new AreaDetectorChangedEvent(areaIn, area);
        RaiseLocalEvent(ent.Owner, ref ev);

        ent.Comp.Area = area;
        Dirty(ent);
    }
}

/// <summary>
/// Raised on the entity with <see cref="AreaDetectorComponent"/> when they enter/exit an area.
/// </summary>
[ByRefEvent]
public record struct AreaDetectorChangedEvent(EntityUid? OldArea, EntityUid? NewArea);
