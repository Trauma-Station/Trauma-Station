// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Wizard.FadingTimedDespawn;

/// <summary>
/// This is a copy of SharedTimedDespawnSystem with some modifications
/// </summary>
public abstract partial class SharedFadingTimedDespawnSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;

    private readonly HashSet<EntityUid> _queuedDespawnEntities = new();

    [SubscribeLocalEvent]
    private void OnStartup(Entity<FadingTimedDespawnComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Timer = Timing.CurTime + ent.Comp.Lifetime;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<FadingTimedDespawnComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.FadeOutStarted)
            FadeOut(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var now = Timing.CurTime;
        _queuedDespawnEntities.Clear();

        var query = EntityQueryEnumerator<FadingTimedDespawnComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!CanDelete(uid))
                continue;

            if (now < comp.Timer)
                continue;

            if (comp.FadeOutTime <= TimeSpan.Zero)
            {
                _queuedDespawnEntities.Add(uid);
                continue;
            }

            if (!comp.FadeOutStarted)
            {
                comp.FadeOutStarted = true;
                comp.Timer += comp.FadeOutTime;
                FadeOut((uid, comp));
                Dirty(uid, comp);
                continue;
            }

            _queuedDespawnEntities.Add(uid);
        }

        foreach (var queued in _queuedDespawnEntities)
        {
            var ev = new TimedDespawnEvent();
            RaiseLocalEvent(queued, ref ev);
            QueueDel(queued);
        }
    }

    protected virtual void FadeOut(Entity<FadingTimedDespawnComponent> ent)
    {
    }

    protected abstract bool CanDelete(EntityUid uid);

    public void FadeDespawnEntity(EntityUid uid, TimeSpan lifetime, TimeSpan fadeOutTime)
    {
        var comp = Factory.GetComponent<FadingTimedDespawnComponent>();
        comp.Lifetime = lifetime;
        comp.FadeOutTime = fadeOutTime;
        AddComp(uid, comp, true);
    }
}
