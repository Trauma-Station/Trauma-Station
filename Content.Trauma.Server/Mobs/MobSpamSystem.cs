// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Shared.Mobs;
using Content.Trauma.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Mobs;

/// <summary>
/// Makes entities with <see cref="MobSpamSystem"/> despawn 5 minutes after dying.
/// </summary>
public sealed class MobSpamSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    public static readonly TimeSpan DespawnTime = TimeSpan.FromMinutes(5);

    private TimedRingBuffer<EntityUid> _buffer = default!;

    public override void Initialize()
    {
        base.Initialize();

        _buffer = new(64, DespawnTime, _timing);

        SubscribeLocalEvent<MobSpamComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_buffer.PopNext(out var uid))
            Despawn(uid);
    }

    private void OnMobStateChanged(Entity<MobSpamComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        QueueDespawn(ent);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _buffer.Reset();
    }

    private void QueueDespawn(EntityUid uid)
    {
        if (_buffer.Push(uid, out var old))
            Despawn(old);
    }

    private void Despawn(EntityUid uid)
    {
        // don't delete mobs that got revived
        if (TerminatingOrDeleted(uid) || !_mob.IsDead(uid))
            return;

        QueueDel(uid);
    }
}
