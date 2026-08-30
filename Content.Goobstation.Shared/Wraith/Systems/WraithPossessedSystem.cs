// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Components;
using Content.Goobstation.Shared.Wraith.Revenant;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Magic.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Wraith.Systems;

/// <summary>
/// This handles getting possessed by the Wraith.
/// This system is hardcoded for Wraith, so don't re-use this.
/// Use the devil system instead. im sorry and sybau im not unhardocoding ts
/// </summary>
public sealed partial class WraithPossessedSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private WraithRevenantSystem _wraithRevenant = default!;
    [Dependency] private ISharedAdminLogManager _admin = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WraithPossessedComponent>();
        foreach (var ent in query)
        {
            if (now < ent.Comp.NextUpdate)
                continue;

            ReturnBack(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<WraithPossessedComponent> ent, ref MapInitEvent args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var mindId, out _))
            return;

        ent.Comp.OriginalMind = mindId;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnDestructionAttempt(Entity<WraithPossessedComponent> ent, ref DestructionAttemptEvent args)
    {
        ReturnBack(ent);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<WraithPossessedComponent> ent, ref MobStateChangedEvent args)
    {
        // early return on death/crit
        if (args.NewMobState != MobState.Alive)
            ReturnBack(ent);
    }

    #region Helpers
    /// <summary>
    /// Starts the possession.
    /// Note: Do not use this if you are not a wraith entity lmao
    /// </summary>
    /// <param name="ent"></param> The entity that is being possessed
    /// <param name="possessor"></param> The possessor
    /// <param name="possessorMind"></param> The possessor's mind
    /// <param name="makeRev"></param> Whether to make the user into a Revenant
    public void StartPossession(Entity<WraithPossessedComponent> ent,
        EntityUid possessor,
        EntityUid possessorMind,
        bool makeRev = false)
    {
        SetPossessorAndMind(ent, possessor, possessorMind);

        var ev = new PossessionStartedEvent();
        RaiseLocalEvent(possessor, ref ev);

        if (makeRev)
        {
            _mind.TransferTo(possessorMind, ent.Owner);
            var rev = EnsureComp<WraithRevenantComponent>(ent.Owner);
            // HELP HELP HELP HELP HELP HELP HELPH ELP HELP HELPHELHPLELHEPL PHLELPHE HELHLEHPELHPELHPELHPELHPELHLEPHLE

            var alive = new List<MobState>();
            alive.Add(MobState.Alive);

            _wraithRevenant.SetPassiveDamageValues((ent.Owner, rev), ent.Comp.RevenantDamageOvertime, alive);

            _admin.Add(LogType.Mind, LogImpact.High, $"{possessor:user} made a revenant (possessed) out of {ent.Owner:target}");
            return;
        }

        // its animateable, no reason to check for anything else
        if (HasComp<AnimateableComponent>(ent.Owner))
        {
            _mind.TransferTo(possessorMind, ent.Owner);

            ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.PossessionDuration;
            Dirty(ent);

            _admin.Add(LogType.Mind, LogImpact.Medium, $"{possessor:user} possessed the object {ent.Owner:target}");
        }
    }

    // TODO: Revenant should get some sort of aura, cosmetic only. Leave for part 2. (lol never happening)
    public void SetPossessorAndMind(
        Entity<WraithPossessedComponent> ent,
        EntityUid possessor,
        EntityUid possessorMind)
    {
        ent.Comp.Possessor = possessor;
        ent.Comp.PossessorMind = possessorMind;
        Dirty(ent);
    }

    public void SetPossessionDuration(Entity<WraithPossessedComponent> ent, TimeSpan duration)
    {
        ent.Comp.PossessionDuration = duration;
        Dirty(ent);
    }

    private void ReturnBack(Entity<WraithPossessedComponent> ent)
    {
        RemCompDeferred(ent, ent.Comp);

        if (ent.Comp.Possessor is not { } user || ent.Comp.PossessorMind is not { } mind)
            return;

        if (TerminatingOrDeleted(user))
        {
            Log.Error($"Tried to return to deleted user of mind {ToPrettyString(mind)} from {ToPrettyString(ent)} to a deleted body!");
            return;
        }

        _mind.TransferTo(mind, user);

        var ev = new PossessionEndedEvent();
        RaiseLocalEvent(user, ref ev);

        if (ent.Comp.OriginalMind is { } originalMind)
            _mind.TransferTo(originalMind, ent.Owner);
    }
    #endregion
}
