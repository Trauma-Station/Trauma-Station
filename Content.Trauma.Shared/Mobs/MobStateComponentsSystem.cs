// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Mobs;

/// <summary>
/// Manages adding and removing per-state components to mobs.
/// </summary>
public sealed class MobStateComponentsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<AliveMobComponent> _aliveQuery;
    private EntityQuery<SleepingComponent> _sleepingQuery;

    public override void Initialize()
    {
        base.Initialize();

        _aliveQuery = GetEntityQuery<AliveMobComponent>();
        _sleepingQuery = GetEntityQuery<SleepingComponent>();

        SubscribeLocalEvent<MobStateComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<AliveMobComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<SoftCritMobComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<CriticalMobComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<DeadMobComponent, MobStateChangedEvent>(OnStateChanged);

        SubscribeLocalEvent<AliveMobComponent, ComponentInit>(OnAliveInit);
        SubscribeLocalEvent<AliveMobComponent, ComponentShutdown>(OnAliveShutdown);
        SubscribeLocalEvent<AliveMobComponent, SleepStateChangedEvent>(OnSleepStateChanged);
    }

    private void OnMapInit(Entity<MobStateComponent> ent, ref MapInitEvent args)
    {
        AddState(ent, ent.Comp.CurrentState);
    }

    private void OnStateChanged<T>(EntityUid uid, T comp, ref MobStateChangedEvent args) where T: Component
    {
        // client would chud out without this
        if (_timing.ApplyingState)
            return;

        // not deferring removal so there is never a tick where multiple state components exist at the same time
        RemComp(uid, comp);
        AddState(uid, args.NewMobState);
    }

    private void OnAliveInit(Entity<AliveMobComponent> ent, ref ComponentInit args)
    {
        if (!_sleepingQuery.HasComp(ent))
            EnsureComp<AwakeMobComponent>(ent);
    }

    private void OnAliveShutdown(Entity<AliveMobComponent> ent, ref ComponentShutdown args)
    {
        // client would chud out without this
        if (_timing.ApplyingState)
            return;

        RemComp<AwakeMobComponent>(ent);
    }

    private void OnSleepStateChanged(Entity<AliveMobComponent> ent, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
            RemComp<AwakeMobComponent>(ent);
        else // woke up while alive, must be awake
            EnsureComp<AwakeMobComponent>(ent);
    }

    public void AddState(EntityUid uid, MobState state)
    {
        switch (state)
        {
            case MobState.Alive:
                EnsureComp<AliveMobComponent>(uid);
                break;
            case MobState.SoftCrit:
                EnsureComp<SoftCritMobComponent>(uid);
                break;
            case MobState.Critical:
                EnsureComp<CriticalMobComponent>(uid);
                break;
            case MobState.Dead:
                EnsureComp<DeadMobComponent>(uid);
                break;
            default:
                break;
        }
    }
}
