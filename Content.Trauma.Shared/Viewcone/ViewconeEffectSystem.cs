// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Movement;
using Content.Trauma.Shared.Viewcone.Components;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Viewcone;

/// <summary>
/// Handles footsteps creating out-of-vision effects.
/// Provides API for spawning viewcone effects and making sure source
/// gets set correctly + it spawns in the correct pos and shit
/// </summary>
public sealed class ViewconeEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ViewconeFootstepsEffectComponent, FootStepEvent>(OnFootStep);
        // TODO: give more sound sources viewcone effects, theres already sprites for gunfire, attacks and speech
    }

    private void OnFootStep(Entity<ViewconeFootstepsEffectComponent> ent, ref FootStepEvent args)
    {
        SpawnEffect(ent, ent.Comp.Effect, args.WorldAngle);
    }

    /// <summary>
    /// Spawns the given effect entity at the player source, and sets relevant variables
    /// </summary>
    /// <param name="source">The player that originated the effect, or the entity to spawn next to if a relevant player doesn't exist</param>
    /// <param name="effect">The prototype ID of an effect entity to spawn (see viewcone_effects.yml)</param>
    /// <param name="angleOverride">The local rotation to set the effect to, instead of the parent rotation.</param>
    public void SpawnEffect(EntityUid source, [ForbidLiteral] EntProtoId effect, Angle? angleOverride = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var ent = PredictedSpawnNextToOrDrop(effect, source);
        var viewconeEffect = EnsureComp<ViewconeOccludableComponent>(ent);
        viewconeEffect.Inverted = true; // it's always visible
        viewconeEffect.Source = source;
        Dirty(ent, viewconeEffect);

        // set rotation
        _xform.SetLocalRotation(ent, angleOverride ?? Transform(source).LocalRotation);

        // also ensure this in case somehow something without it gets here.
        EnsureComp<TimedDespawnComponent>(ent);
    }
}
