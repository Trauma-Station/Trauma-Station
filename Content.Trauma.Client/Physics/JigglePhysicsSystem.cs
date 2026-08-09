// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.DisplacementMap;
using Content.Shared.DisplacementMap;
using Content.Trauma.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Client.Physics;

/// <summary>
/// Jiggle physics inspired by the Fox Engine used in Metal Gear Solid V: The Phantom Pain created and directed by Hideo Kojima.
/// </summary>
public sealed partial class JigglePhysicsSystem : EntitySystem
{
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private SpriteSystem _sprite = default!;
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

        var vis = EnsureComp<JigglePhysicsVisualsComponent>(ent);
        foreach (var sourceKey in ent.Comp.Layers)
        {
            var index = _sprite.LayerMapReserve((ent, sprite), sourceKey);
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

        var sign = parentAccel.X < 0 ? 1f : -1f;
        var inertia = sign * parentAccel.Length();
        var accel = SpringAcceleration(comp, vis.Jiggle) - inertia;
        // probably not mathematically correct integration but dt is usually small
        vis.Slap += accel * dt;
        vis.Slap = Math.Clamp(vis.Slap, -comp.JiggleLimit, comp.JiggleLimit);
        vis.Jiggle += vis.Slap * dt;
        vis.Jiggle = Math.Clamp(vis.Jiggle, -comp.JiggleLimit, comp.JiggleLimit);

        vis.LastParentVelocity = vel;

        var number = NextDisplacementNumber(comp, vis.Jiggle);
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
        => (int) (jiggle * comp.DisplacementCount / comp.JiggleLimit);

    private float SpringAcceleration(JigglePhysicsComponent comp, float jiggle)
        => -comp.Springiness * jiggle;
}
