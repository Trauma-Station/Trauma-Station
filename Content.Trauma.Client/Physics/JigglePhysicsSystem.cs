// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Client.Physics;

/// <summary>
/// Jiggle physics inspired by the Fox Engine used in Metal Gear Solid V: The Phantom Pain created and directed by Hideo Kojima.
/// </summary>
public sealed partial class JigglePhysicsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<JigglePhysicsVisualsComponent> _visualsQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    private static readonly ProtoId<ShaderPrototype> DisplacedDraw = "DisplacedDraw";

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = ProtoMan.Index(DisplacedDraw).Instance();
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<JigglePhysicsComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        var vis = EnsureComp<JigglePhysicsVisualsComponent>(ent);
        var state = ent.Comp.DisplacementPrefix + 1;
        var index = _sprite.LayerMapGet((ent, sprite), ent.Comp.LayerKey);
        _sprite.AddRsiLayer((ent, sprite), state, ent.Comp.DisplacementsRsi, index);
        _sprite.TryGetLayer((ent, sprite), index, out var layer, true);
        _sprite.LayerSetVisible(layer!, false);
        sprite.LayerSetShader(index, _shader);
        layer.CopyToShaderParameters = new(ent.Comp.LayerKey)
        {
            ParameterTexture = "displacementMap",
            ParameterUV = "displacementUV"
        };
        vis.Layer = layer;
    }

    [SubscribeLocalEvent]
    private void OnAutoHandleState(Entity<JigglePhysicsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_visualsQuery.TryComp(ent, out var vis))
            return;

        _sprite.LayerSetRsi(vis.Layer, ent.Comp.DisplacementsRsi);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<JigglePhysicsComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _sprite.RemoveLayer(ent.Owner, JigglePhysicsVisuals.Layer);
        RemComp<JigglePhysicsVisualsComponent>(ent);
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

        var active = number != 0;
        var layer = quiet.Comp2.Layer;
        _sprite.LayerSetVisible(layer, active);
        if (!active)
            return;

        var state = quiet.Comp1.DisplacementPrefix + number;
        _sprite.LayerSetRsiState(layer, state);
    }

    private int NextDisplacementNumber(JigglePhysicsComponent comp, float jiggle)
        => (int) (jiggle * comp.DisplacementCount / comp.JiggleLimit);

    private float SpringAcceleration(JigglePhysicsComponent comp, float jiggle)
        => -comp.Springiness * jiggle;
}
