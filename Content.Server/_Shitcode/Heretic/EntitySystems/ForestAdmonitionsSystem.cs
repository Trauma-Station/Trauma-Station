using System.Linq;
using System.Numerics;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server._Shitcode.Heretic.EntitySystems;

public sealed class ForestAdmonitionsSystem : SharedForestAdmonitionsSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly HashSet<Entity<PhysicsComponent>> _lookupPhysics = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<ShadowCloakedComponent, ForestAdmonitionsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var cloaked, out var forest, out var xform))
        {
            if (cloaked.ShadowCloakEntity != forest.CloakEntity)
                continue;

            if (now < forest.NextUpdate)
                continue;

            forest.NextUpdate = now + forest.UpdateDelay;
            SpreadFog((uid, forest, xform));
        }
    }

    private void SpreadFog(Entity<ForestAdmonitionsComponent, TransformComponent> ent)
    {
        var (_, forest, xform) = ent;

        var range = forest.Range;
        var limit = new Vector2(range).Length();
        var inv = 1f / limit;
        var coords = xform.Coordinates;
        for (var y = -range; y <= range; y++)
        {
            for (var x = -range; x <= range; x++)
            {
                var offset = new Vector2(x, y);
                var length = offset.Length() * inv;
                var chance = MathF.Pow(1f - length, forest.FogSlope);

                if (!_random.Prob(Math.Clamp(chance, 0f, 1f)))
                    continue;

                var pos = coords.Offset(offset).SnapToGrid(EntityManager, _mapMan);
                var mapPos = XForm.ToMapCoordinates(pos);

                if (offset != Vector2.Zero && !_mapMan.TryFindGridAt(mapPos, out _, out _))
                    continue;

                const int mask = (int) (CollisionGroup.Impassable | CollisionGroup.HighImpassable);

                _lookupPhysics.Clear();
                _lookup.GetEntitiesInRange(mapPos, 0.1f, _lookupPhysics);
                if (_lookupPhysics.Any(e => (e.Comp.CollisionLayer & mask) != 0))
                    continue;

                Spawn(forest.FogProto, mapPos);
            }
        }
    }
}
