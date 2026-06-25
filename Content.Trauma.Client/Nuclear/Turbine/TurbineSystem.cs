// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Repairable;
using Content.Trauma.Shared.Nuclear.Turbine;
using Robust.Client.Animations;

namespace Content.Trauma.Client.Nuclear.Reactor;

public sealed partial class TurbineSystem : SharedTurbineSystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    // TODO: kill
    private readonly float _threshold = 1f;
    private float _accumulator = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurbineComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    #region Animation
    private void OnAnimationCompleted(Entity<TurbineComponent> ent, ref AnimationCompletedEvent args)
    {
        PlayAnimation(ent);
    }

    public override void FrameUpdate(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator >= _threshold)
        {
            AccUpdate();
            _accumulator = 0;
        }
    }

    private void AccUpdate()
    {
        var query = EntityQueryEnumerator<TurbineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Makes sure the anim doesn't get stuck at low RPM
            PlayAnimation((uid, comp));
        }
    }

    private void PlayAnimation(Entity<TurbineComponent> ent)
    {
        var (uid, comp) = ent;
        if (!_sprite.TryGetLayer(uid, TurbineVisualLayers.TurbineSpeed, out var layer, true))
            return;

        var state = "speedanim";
        if (comp.RPM < 1)
        {
            _animationPlayer.Stop(uid, state);
            _sprite.LayerSetRsiState(layer, "turbine");
            comp.AnimRPM = -comp.BestRPM; // Primes it to start the instant it's spinning again
            return;
        }

        if (Math.Abs(comp.RPM - comp.AnimRPM) > comp.BestRPM * 0.1)
            _animationPlayer.Stop(uid, state); // Current anim is stale, time for a new one

        if (_animationPlayer.HasRunningAnimation(uid, state))
            return;

        comp.AnimRPM = comp.RPM;
        var layerKey = TurbineVisualLayers.TurbineSpeed;
        var time = 0.5f * comp.BestRPM / comp.RPM;
        var timestep = time / 12;
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(time),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = layerKey,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_00", 0),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_01", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_02", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_03", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_04", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_05", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_06", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_07", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_08", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_09", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_10", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_11", timestep)
                    }
                }
            }
        };
        _sprite.LayerSetVisible(layer, true);
        _animationPlayer.Play(uid, animation, state);
    }
    #endregion
}
