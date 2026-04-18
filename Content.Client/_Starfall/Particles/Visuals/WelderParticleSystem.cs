using Content.Shared._Starfall.Particles;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Starfall.Particles;

public sealed class WelderParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleEffectPrototype> SparksEffect = "SfWeldingSparks";

    private sealed class WeldState
    {
        public ActiveEmitter? SparksEmitter;
    }

    private readonly Dictionary<EntityUid, WeldState> _active = new();
    private readonly HashSet<EntityUid> _currentlyWelding = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeldableComponent, ComponentShutdown>(OnShutdown);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _currentlyWelding.Clear();

        var doAfterQuery = EntityQueryEnumerator<DoAfterComponent>();
        while (doAfterQuery.MoveNext(out _, out var doAfterComp))
        {
            foreach (var (_, doAfter) in doAfterComp.DoAfters)
            {
                if (doAfter.Cancelled || doAfter.Completed)
                    continue;

                if (doAfter.Args.Used is not { } usedTool || !HasComp<WelderComponent>(usedTool))
                    continue;

                if (doAfter.Args.Target is not { } weldTarget)
                    continue;

                _currentlyWelding.Add(weldTarget);
            }
        }

        foreach (var target in _currentlyWelding)
        {
            if (!_active.TryGetValue(target, out var state))
            {
                state = new WeldState();
                _active[target] = state;
            }

            if (state.SparksEmitter == null)
            {
                var coords = _transform.GetMapCoordinates(target);
                state.SparksEmitter = _particles.SpawnEffect(SparksEffect, coords, target);
            }
        }

        foreach (var (target, state) in _active)
        {
            if (!_currentlyWelding.Contains(target) && state.SparksEmitter != null)
            {
                state.SparksEmitter.Exhausted = true;
                state.SparksEmitter = null;
            }
        }
    }

    private void OnShutdown(Entity<WeldableComponent> ent, ref ComponentShutdown args)
    {
        if (!_active.Remove(ent, out var state))
            return;

        if (state.SparksEmitter != null)
        {
            state.SparksEmitter.Exhausted = true;
            state.SparksEmitter = null;
        }
    }
}

