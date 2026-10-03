// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.DisplacementMap;
using Content.Shared.Camera;
using Content.Shared.DisplacementMap;
using Content.Trauma.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Reflection;

namespace Content.Trauma.Client.Physics;

/// <summary>
/// Jiggle physics inspired by the Fox Engine used in Metal Gear Solid V: The Phantom Pain created and directed by Hideo Kojima.
/// </summary>
public sealed partial class JigglePhysicsSystem : EntitySystem
{
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<CameraRecoilComponent> _recoilQuery = default!;
    [Dependency] private EntityQuery<JigglePhysicsVisualsComponent> _visualsQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    private DisplacementData _data = new();

    [SubscribeLocalEvent]
    private void OnStartup(Entity<JigglePhysicsComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        _data.SizeMaps[32] = new()
        {
            RsiPath = ent.Comp.DisplacementsRsi.ToString(),
            State = ent.Comp.DisplacementPrefix + "0"
        };

        // TODO: support changing every layer
        var vis = EnsureComp<JigglePhysicsVisualsComponent>(ent);
        foreach (var keyName in ent.Comp.Layers)
        {
            int index;
            object sourceKey = keyName;
            if (_reflection.TryParseEnumReference(keyName, out var keyEnum))
            {
                index = _sprite.LayerMapReserve((ent, sprite), keyEnum);
                sourceKey = keyEnum;
            }
            else
            {
                index = _sprite.LayerMapReserve((ent, sprite), keyName);
            }

            if (!_displacement.TryAddDisplacement(_data, (ent, sprite), index, sourceKey, out var key))
                continue;

            if (_sprite.TryGetLayer((ent, sprite), key, out var layer, true))
                vis.Layers.Add(layer);
        }
    }

    [SubscribeLocalEvent]
    private void OnAutoHandleState(Entity<JigglePhysicsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_visualsQuery.TryComp(ent, out var vis))
            return;

        foreach (var layer in vis.Layers)
        {
            _sprite.LayerSetRsi(layer, ent.Comp.DisplacementsRsi);
        }
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<JigglePhysicsComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemComp<JigglePhysicsVisualsComponent>(ent);

        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        var spriteEnt = new Entity<SpriteComponent>(ent, sprite);
        foreach (var key in ent.Comp.Layers)
        {
            _displacement.EnsureDisplacementIsNotOnSprite(spriteEnt, key);
        }
    }

    public override void FrameUpdate(float dt)
    {
        base.FrameUpdate(dt);

        var query = EntityQueryEnumerator<JigglePhysicsComponent, JigglePhysicsVisualsComponent, PhysicsComponent>();
        foreach (var quiet in query)
        {
            Jiggle(quiet, dt);
        }
    }

    private void Jiggle(Entity<JigglePhysicsComponent, JigglePhysicsVisualsComponent, PhysicsComponent> quiet, float dt)
    {
        var (uid, comp, vis, phys) = quiet;
        var vel = phys.LinearVelocity;
        var parentAccel = (vel - vis.LastParentVelocity) / dt;

        var spring = vis.Jiggle * comp.Springiness;
        var damping = vis.Slap * comp.Damping;
        var external = parentAccel * comp.InertiaScale;
        var force = spring + damping + external;
        // probably not mathematically correct integration but dt is usually small
        vis.Slap -= force * dt; // assume mass is 1 :^)
        ClampVector(ref vis.Slap, comp.SlapLimit);
        var dJiggle = vis.Slap;
        if (_recoilQuery.TryComp(uid, out var recoil))
            dJiggle += recoil.CurrentKick; // mgsv parity
        vis.Jiggle += dJiggle * dt;
        ClampVector(ref vis.Jiggle, comp.JiggleLimit);

        vis.LastParentVelocity = vel;

        // TODO: when X is very small and sign oscillates this needs to have a stable direction dependent on Y
        // both axes affect jiggle so moving up/down still jiggles, the alternative of having n^2 displacements is super hell
        var dir = vis.Jiggle.X >= 0f ? -1f : 1f;
        var jiggle1d = vis.Jiggle.Length() * dir;
        var number = NextDisplacementNumber(comp, jiggle1d);
        if (number != vis.DisplacementNumber)
        {
            vis.DisplacementNumber = number;
            UpdateDisplacement((uid, comp, vis), number);
        }
    }

    private void UpdateDisplacement(Entity<JigglePhysicsComponent, JigglePhysicsVisualsComponent> quiet, int number)
    {
        if (!_spriteQuery.TryComp(quiet, out var sprite))
            return;

        var state = quiet.Comp1.DisplacementPrefix + number;
        foreach (var layer in quiet.Comp2.Layers)
        {
            _sprite.LayerSetRsiState(layer, state);
        }
    }

    private int NextDisplacementNumber(JigglePhysicsComponent comp, float jiggle)
        => (int) Math.Round(jiggle * comp.DisplacementCount / comp.JiggleLimit);

    /// <summary>
    /// Circular clamping of a vector's length by scaling it down.
    /// Maintains <c>Length</c> limit compared to <c>Vector2.Clamp</c> which only limits X/Y.
    /// </summary>
    private void ClampVector(ref Vector2 vector, float limit)
    {
        var len2 = vector.LengthSquared();
        if (len2 <= limit * limit)
            return;

        vector *= limit / MathF.Sqrt(len2);
    }
}
