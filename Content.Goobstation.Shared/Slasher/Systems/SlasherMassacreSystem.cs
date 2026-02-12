using Content.Goobstation.Shared.Slasher.Components;
using Content.Goobstation.Shared.Slasher.Events;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Wounds;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Content.Shared.Humanoid;

namespace Content.Goobstation.Shared.Slasher.Systems;

/// <summary>
/// System for the massacre action. Can be re-used for other similar weapons(Like the gang weapon if that ever gets made).
/// </summary>
public sealed class SlasherMassacreSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherMassacreMacheteComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<SlasherMassacreUserComponent, SlasherMassacreEvent>(OnMassacreAction);
        SubscribeLocalEvent<SlasherMassacreMacheteComponent, MeleeHitEvent>(OnMeleeHitWeapon);
    }

    private void OnGetItemActions(EntityUid uid, SlasherMassacreMacheteComponent comp, GetItemActionsEvent args)
    {
        EnsureComp<SlasherMassacreUserComponent>(args.User);

        if (_net.IsServer)
            args.AddAction(ref comp.MassacreActionEntity, comp.MassacreActionId);

        Dirty(uid, comp);
    }

    private void OnMassacreAction(Entity<SlasherMassacreUserComponent> ent, ref SlasherMassacreEvent args)
    {
        if (!_net.IsServer)
        {
            args.Handled = true;
            return;
        }

        if (!ent.Comp.Active)
        {
            ent.Comp.Active = true;
            ent.Comp.HitCount = 0;
            ent.Comp.CurrentVictim = null;

            _popup.PopupEntity(Loc.GetString("slasher-massacre-start"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            _audio.PlayPvs(ent.Comp.MassacreIntro, ent.Owner);

        } // better formatting :shrug:
        else
            EndChain(ent.Owner, ent.Comp, showPopup: true);

        args.Handled = true;
        Dirty(ent);
    }

    private void EndChain(EntityUid uid, SlasherMassacreUserComponent comp, bool showPopup = false)
    {
        if (comp.Active && showPopup && _net.IsServer)
            _popup.PopupEntity(Loc.GetString("slasher-massacre-end"), uid, uid, PopupType.MediumCaution);

        comp.Active = false;
        comp.HitCount = 0;
        comp.CurrentVictim = null;
        Dirty(uid, comp);
    }

    private void OnMeleeHitWeapon(Entity<SlasherMassacreMacheteComponent> weaponEnt, ref MeleeHitEvent args)
    {
        if (!TryComp<SlasherMassacreUserComponent>(args.User, out var userComp)
            || !userComp.Active) // don't activate when comp isn't active.
            return;

        // End the chain when you miss.
        if (args.HitEntities.Count == 0)
        {
            EndChain(args.User, userComp, true);
            return;
        }

        // Only consider humanoids as targets.
        var victim = EntityUid.Invalid;
        foreach (var hit in args.HitEntities)
        {
            if (!HasComp<HumanoidProfileComponent>(hit))
                continue;

            victim = hit;
            break;
        }

        // If no valid humanoid was hit, treat like a miss and end the chain.
        if (victim == EntityUid.Invalid)
        {
            EndChain(args.User, userComp, true);
            return;
        }

        // When the target changes reset hitcount.
        if (userComp.CurrentVictim != null && userComp.CurrentVictim != victim)
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("slasher-massacre-target-change"), args.User, args.User, PopupType.MediumCaution);

            userComp.HitCount = 0;
        }

        userComp.CurrentVictim = victim;
        userComp.HitCount++;

        // Calculate damage bonus/penalty.
        var totalBonus = -weaponEnt.Comp.BaseDamagePenalty + weaponEnt.Comp.PerHitBonus * (userComp.HitCount - 1);
        if (totalBonus != 0)
        {
            var spec = new DamageSpecifier();
            spec.DamageDict.Add("Slash", totalBonus);
            args.BonusDamage += spec;
        }

        // If the victim died end the chain silently.
        if (_mobState.IsDead(victim))
        {
            EndChain(args.User, userComp);
            return;
        }

        var playedDelimb = false;

        // Limb severing phase.
        if (userComp.HitCount >= weaponEnt.Comp.LimbSeverHits)
        {
            if (TrySeverRandomLimb(victim, chance: weaponEnt.Comp.LimbSeverChance))
                playedDelimb = true;
        }

        // Decapitation.
        if (userComp.HitCount == weaponEnt.Comp.DecapitateHit)
        {
            if (_body.TryDecapitate(victim, args.User))
            {
                playedDelimb = true;
                _popup.PopupPredicted(Loc.GetString("slasher-massacre-decap"), victim, args.User, PopupType.Large);
            }
            EndChain(args.User, userComp);
        }

        // Audio handling
        if (_net.IsServer)
        {
            if (playedDelimb)
                _audio.PlayPvs(weaponEnt.Comp.MassacreDelimb, args.User);
            else
                _audio.PlayPvs(weaponEnt.Comp.MassacreSlash, args.User);
        }

        Dirty(args.User, userComp);
    }

    // Handles severing a random limb.
    private bool TrySeverRandomLimb(EntityUid victim, float chance)
    {
        if (_net.IsServer) // TODO: use predicted random ffs
            return false;

        if (!_random.Prob(chance))
            return false;

        var severable = _body.GetOrgans<LimbComponent>(victim);
        if (severable.Count == 0)
            return false;

        var pickedLimb = _random.Pick(severable);

        if (!TryComp<WoundableComponent>(pickedLimb, out var woundable) || woundable.ParentWoundable is not {} parent)
            return false;

        _wound.AmputateWoundableSafely(parent, pickedLimb, woundable);

        _popup.PopupEntity(Loc.GetString("slasher-massacre-limb"), victim, PopupType.Medium);
        return true;
    }
}
