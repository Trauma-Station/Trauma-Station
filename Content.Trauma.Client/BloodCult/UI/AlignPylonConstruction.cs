// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.BloodCult.Pylon;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
using Robust.Shared.Map;

namespace Content.Trauma.Client.BloodCult.UI;

// FIXME: this is purely clientside, malf clients can ignore this
public sealed class AlignPylonConstruction : SnapgridCenter
{
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _transform = default!;

    private HashSet<Entity<PylonComponent>> _pylons = new();

    private const float PylonLookupRange = 10;

    public AlignPylonConstruction(PlacementManager pMan) : base(pMan)
    {
        var entMan = pMan.EntityManager;
        _lookup = entMan.System<EntityLookupSystem>();
        _transform = entMan.System<SharedTransformSystem>();
    }

    public override bool IsValidPosition(EntityCoordinates position)
        => base.IsValidPosition(position) && NoNearbyPylons(position, PylonLookupRange);

    private bool NoNearbyPylons(EntityCoordinates pos, float range)
    {
        _pylons.Clear();
        _lookup.GetEntitiesInRange(pos, range, _pylons);
        return _pylons.Count == 0;
    }
}
