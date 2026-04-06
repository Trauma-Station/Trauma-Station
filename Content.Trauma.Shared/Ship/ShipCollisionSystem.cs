using System.Linq;
using System.Numerics;
using Content.Shared.Physics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Content.Trauma.Shared.Ship;

public sealed partial class ShipCollisionSystem : EntitySystem
{
   [Dependency] private readonly SharedPhysicsSystem _physics = default!;
   [Dependency] private readonly FixtureSystem _fixtures = default!;

   public override void Initialize()
   {
       base.Initialize();

       SubscribeLocalEvent<MapGridComponent, GridInitializeEvent>(OnGridInit, after: [typeof(SharedGridFixtureSystem)]);
       SubscribeLocalEvent<FixturesComponent, MapInitEvent>(OnFixtureAdded, after: [typeof(SharedGridFixtureSystem)]);
       //SubscribeLocalEvent<MapGridComponent, UserUnanchoredEvent>(OnFixtureDeadded, after: [typeof(SharedGridFixtureSystem)]);
   }

   private void OnGridInit(EntityUid uid, MapGridComponent component, GridInitializeEvent args)
   {
       // Apply the fixture logic to the new station
       DisableGridCollision(uid);
   }

   private void DisableGridCollision(EntityUid uid, FixturesComponent? manager = null)
   {
       if (!Resolve(uid, ref manager))
           return;

       foreach (var (name, fixture) in manager.Fixtures)
       {
           _physics.SetHard(uid, fixture, false, manager);
           //_physics.SetCollisionLayer(uid, name, fixture, (int) CollisionGroup.None, manager);
           //_physics.SetCollisionMask(uid, name, fixture, 0, manager);
       }
   }

   private void OnFixtureAdded(EntityUid uid, FixturesComponent component, MapInitEvent args)
   {
       AddFixture(uid, component);
   }

   private void AddFixture(EntityUid uid, FixturesComponent component)
   {

       var xform = Transform(uid);
       if (xform.GridUid == null)
           return;

       var gridUid = xform.GridUid.Value;

       if (uid == gridUid)
           return;

       if (!TryComp<FixturesComponent>(gridUid, out var gridFixtures))
           return;

       var pos = xform.LocalPosition;
       var rot = xform.LocalRotation;

       var fixtureList = component.Fixtures.ToList();

       foreach (var (id, fixture) in fixtureList)
       {
           var uniqueId = $"anchored_{uid}_{id}";

           if (gridFixtures.Fixtures.ContainsKey(uniqueId))
               continue;

           var shape = fixture.Shape;

           // If it's a PolygonShape (most walls), we need to shift the vertices
           if (shape is PolygonShape poly)
           {
               var newVertices = new Vector2[poly.Vertices.Length];

               for (var i = 0; i < poly.Vertices.Length; i++)
               {
                   var rotated = rot.RotateVec(poly.Vertices[i]);
                   newVertices[i] = rotated + pos;
               }

               var newPoly = new PolygonShape();
               newPoly.Set(newVertices, newVertices.Length);
               shape = newPoly;
           }

           _fixtures.TryCreateFixture(
               gridUid,
               shape,
               uniqueId,
               //fixture.Density,
               0.1f,
               true,
               (int) CollisionGroup.MapGrid,
               (int) CollisionGroup.MapGrid,
               fixture.Friction,
               fixture.Restitution,
               manager: gridFixtures);
           _fixtures.TryCreateFixture(gridUid, fixture.Shape, uniqueId, fixture.Density, fixture.Hard, fixture.CollisionLayer, fixture.CollisionMask, fixture.Friction, fixture.Restitution, manager: gridFixtures);
       }
   }
}
