using System.Numerics;
using Content.Server._Mono.FireControl;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.Spawners;

namespace Content.Server._Mono.Radar;

/// <summary>
/// System that handles radar visualization for hitscan projectiles
/// </summary>
public sealed partial class HitscanRadarSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<HitscanFireEffectEvent>(OnHitscanEffect); // TODO convert to work with new hitscan system
        SubscribeLocalEvent<HitscanRadarComponent, ComponentShutdown>(OnHitscanRadarShutdown);
    }

    /* TODO convert to work with new hitscan system
    private void OnHitscanEffect(HitscanFireEffectEvent ev)
    {
        if (ev.Shooter is not {} shooter)
            return;

        // Only create hitscan radar blips for entities with FireControllable component
        if (!HasComp<FireControllableComponent>(shooter))
            return;

        // Create a new entity for the hitscan radar visualization
        // Use the shooter's position to spawn the entity
        var shooterCoords = new EntityCoordinates(shooter, Vector2.Zero);
        var uid = Spawn(null, shooterCoords);

        // Add the hitscan radar component
        var hitscanRadar = EnsureComp<HitscanRadarComponent>(uid);

        // Determine start position using proper coordinate transformation
        var startPos = _transform.ToMapCoordinates(ev.FromCoordinates).Position;

        // Compute end position in map space (world coordinates)
        var dir = ev.Angle.ToVec().Normalized();
        var endPos = startPos + dir * ev.Distance;

        // Set the origin grid if available
        hitscanRadar.OriginGrid = Transform(shooter).GridUid;

        // Set the start and end coordinates
        hitscanRadar.StartPosition = startPos;
        hitscanRadar.EndPosition = endPos;

        // Inherit component settings from the shooter entity
        InheritShooterSettings(shooter, hitscanRadar, ev.Hitscan);

        // Schedule entity for deletion after its lifetime expires
        EnsureComp<TimedDespawnComponent>(uid).Lifetime = hitscanRadar.LifeTime;
    } */

    /// <summary>
    /// Inherits radar settings from the shooter entity if available
    /// </summary>
    private void InheritShooterSettings(EntityUid shooter, HitscanRadarComponent hitscanRadar)
    {
        // Try to inherit from shooter's existing HitscanRadarComponent if present
        if (TryComp<HitscanRadarComponent>(shooter, out var shooterHitscanRadar))
        {
            hitscanRadar.RadarColor = shooterHitscanRadar.RadarColor;
            hitscanRadar.LineThickness = shooterHitscanRadar.LineThickness;
            hitscanRadar.Enabled = shooterHitscanRadar.Enabled;
            hitscanRadar.LifeTime = shooterHitscanRadar.LifeTime;
        }
    }

    private void OnHitscanRadarShutdown(Entity<HitscanRadarComponent> ent, ref ComponentShutdown args)
    {
        // Only delete the entity if it's a temporary hitscan trail entity
        // Don't delete legitimate entities that have the component added manually
        if (HasComp<TimedDespawnComponent>(ent))
            QueueDel(ent);
    }
}
