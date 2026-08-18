// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SlaughterDemon.Objectives;
using Content.Goobstation.Shared.SlaughterDemon.Other;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.SlaughterDemon.Systems;

/// <summary>
/// This handles the devouring system for the slaughter demons
/// </summary>
public sealed partial class SlaughterDevourSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private EntityQuery<PullerComponent> _pullerQuery = default!;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SlaughterDevourComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent.Owner, "stomach");
    }

    [SubscribeLocalEvent]
    private void OnBloodCrawlAttempt(Entity<SlaughterDevourComponent> ent, ref BloodCrawlAttemptEvent args)
    {
        if (_pullerQuery.CompOrNull(ent)?.Pulling is not { } pulled)
            return;

        if (_mobState.IsAlive(pulled))
            return;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            ent,
            ent.Comp.DoAfterDelay,
            new SlaughterDevourDoAfterEvent(),
            ent,
            pulled)
        {
            BreakOnMove = true,
            ColorOverride = Color.Red
        };

        // cancel the jaunt and devour instead
        args.Cancelled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    #region Drink-related

    // TODO: make this a status effect comp jesus christ
    [SubscribeLocalEvent]
    private void OnAttemptDemonsBlood(Entity<DemonsBloodComponent> ent, ref SlaughterDevourAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        _popup.PopupEntity(Loc.GetString("slaughter-demons-blood-devour"), args.Devourer, args.Devourer, PopupType.SmallCaution);
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnAttemptDemonsKiss(Entity<DemonsKissComponent> ent, ref SlaughterDevourAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        _damageable.TryChangeDamage(args.Devourer, ent.Comp.Damage, ignoreResistances: true);
        _popup.PopupEntity(Loc.GetString("slaughter-demons-kiss-devour"), args.Devourer, args.Devourer, PopupType.MediumCaution);

        if (ent.Comp.Eject)
            args.Cancelled = true;
    }
    #endregion

    public void HealAfterDevouring(Entity<SlaughterDevourComponent> ent, EntityUid target)
    {
        var popup = "slaughter-devour-other";
        var amount = ent.Comp.ToHealAnythingElse;
        // I dont know how to refactor this into events so im leaving it like this
        // W sped
        if (_whitelist.IsWhitelistPass(ent.Comp.RobotWhitelist, target))
        {
            popup = "slaughter-devour-robot";
            amount = ent.Comp.ToHealNonCrew;
        }
        else if (_humanoidQuery.HasComp(target))
        {
            popup = "slaughter-devour-humanoid";
            amount = ent.Comp.ToHeal;
        }

        _popup.PopupEntity(Loc.GetString(popup), ent, ent);
        var damage = ent.Comp.HealDamage * amount;
        _damageable.ChangeDamage(ent.Owner, damage, true);
    }

    /// <summary>
    ///  Increments the objectives of the slaughter demons
    /// </summary>
    public void IncrementObjective(Entity<SlaughterDemonComponent> ent, EntityUid target)
    {
        if (!_humanoidQuery.HasComp(target) || // cant eat mice
            !_mind.TryGetMind(target, out _, out _) || // cant eat salv unidentified corpses etc
            !_mind.TryGetMind(ent, out _, out var mind))
            return;

        // Goidaaaaaa
        foreach (var objective in mind.Objectives)
        {
            if (TryComp<SlaughterDevourConditionComponent>(objective, out var devourCondition))
                devourCondition.Devour = ent.Comp.Devoured;

            if (TryComp<SlaughterKillEveryoneConditionComponent>(objective, out var killEveryoneCondition))
                killEveryoneCondition.Devoured++;
        }
    }
}
