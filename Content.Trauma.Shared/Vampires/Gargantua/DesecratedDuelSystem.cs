// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Arena;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Gargantua;

public sealed class DesecratedDuelSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ArenaCreationSystem _arena = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private static readonly EntProtoId<StatusEffectComponent> StatusEffectGladiator = "StatusEffectGladiator";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionDesecratedDuelComponent, ArenaTargetActionEvent>(OnPerform);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var activeQuery = EntityQueryEnumerator<ActiveActionDesecratedDuelComponent, ActionDesecratedDuelComponent>();
        while (activeQuery.MoveNext(out var uid, out var active, out var duel))
        {
            // This time check ensures the arena gets deleted after a certain amount of time.
            if (active.DuelCheck < now)
            {
               ExitArena(uid, duel.Duelist);
               RemCompDeferred(uid, active);
               continue;
            }

            // This time check ensures the arena gets deleted if either of the fighters has died.
            if (active.NextFighterCheck < now)
            {
                CheckDuelist(uid, duel.Duelist);
                RemCompDeferred(uid, active);
            }
        }
    }

    private void OnPerform(Entity<ActionDesecratedDuelComponent> ent, ref ArenaTargetActionEvent args)
    {
        // Note: some other stuff (status effects) are done with EffectActionComponent. Check the yml of the action to see what.
        var performer = args.Performer;
        var target = args.Target;

        // First, we leap towards our target
        _throw.TryThrow(performer, Transform(target).Coordinates, 30f, performer);

        // Set the duelist
        ent.Comp.Duelist = performer;
        Dirty(ent);

        // Set the active timers
        var now = _timing.CurTime;
        var comp = new ActiveActionDesecratedDuelComponent();
        comp.DuelCheck = now + ent.Comp.DuelDuration;
        comp.NextFighterCheck = now + ent.Comp.FighterCheck;
        AddComp(ent.Owner, comp, true);
    }

    #region Helpers

    /// <summary>
    /// Clears the arena and anything related to it.
    /// </summary>
    private void ExitArena(EntityUid uid, EntityUid duelist)
    {
        _arena.DestroyArena(uid);

        // Clear the status effect
        _status.TryRemoveStatusEffect(duelist, StatusEffectGladiator);
    }

    /// <summary>
    /// Check on the duelist to see if they are alive or deleted.
    /// Exits the arena if duelist is dead or deleted.
    /// </summary>
    private void CheckDuelist(EntityUid action, EntityUid target)
    {
        if (!TerminatingOrDeleted(target) || !_mob.IsDead(target))
            return;

        ExitArena(action, target);
    }
    #endregion
}
