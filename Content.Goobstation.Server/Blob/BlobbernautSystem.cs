// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Blob;

public sealed partial class BlobbernautSystem : SharedBlobbernautSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<BlobTileComponent> _tileQuery = default!;
    [Dependency] private EntityQuery<BlobCoreComponent> _coreQuery = default!;

    private readonly HashSet<Entity<BlobTileComponent>> _tiles = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BlobbernautComponent>();
        foreach (var ent in query)
        {
            if (_mob.IsDead(ent.Owner))
                continue;

            var comp = ent.Comp;
            if (now < comp.NextDamage)
                continue;

            comp.NextDamage = now + comp.DamageDelay;

            if (TerminatingOrDeleted(comp.Factory))
            {
                TryChangeDamage("blobberaut-factory-destroy", ent, comp.Damage);
                continue;
            }

            var xform = Transform(ent);
            if (xform.GridUid == null)
                continue;

            _tiles.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, 1f, _tiles);
            if (_tiles.Count != 0)
                continue;

            TryChangeDamage("blobberaut-not-on-blob-tile", ent, comp.Damage);
        }
    }

    private DamageSpecifier TryChangeDamage(string msg, EntityUid ent, DamageSpecifier dmg)
    {
        _popup.PopupEntity(Loc.GetString(msg), ent, ent, PopupType.LargeCaution);
        return _damage.ChangeDamage(ent, dmg);
    }
}
