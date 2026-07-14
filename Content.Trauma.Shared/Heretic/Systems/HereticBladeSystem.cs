// SPDX-License-Identifier: AGPL-3.0-or-later


using System.Linq;
using Content.Goobstation.Common.BlockTeleport;
using Content.Goobstation.Common.Weapons;
using Content.Goobstation.Shared.Boomerang;
using Content.Shared.Actions.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Throwing;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Teleportation.Systems;
using Content.Trauma.Shared.Wizard.SanguineStrike;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed partial class HereticBladeSystem : EntitySystem
{
    [Dependency] private CosmosComboSystem _combo = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private RandomTeleportSystem _teleport = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedHereticCombatMarkSystem _combatMark = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSanguineStrikeSystem _sanguine = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedChargesSystem _charges = default!;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private EntityQuery<HereticActionBladeBreakRechargeComponent> _bladeBreakRechargeQuery = default!;
    [Dependency] private EntityQuery<LimitedChargesComponent> _limitedQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticBladeComponent, BeforeDamageOtherOnHitEvent>(OnBeforeThrowDamage,
            after: new[] { typeof(BoomerangSystem) });
    }

    [SubscribeLocalEvent]
    private void OnBladeBlade(Entity<HereticBladeComponent> ent, ref BladeBladeBonusEvent args)
    {
        args.BonusDamage += args.ExtraDamage;

        var user = args.User;

        if (!TryComp(user, out SilverMaelstromComponent? maelstrom))
            return;

        var aliveMobsCount = args.HitEntities.Count(x => x != user && _mobState.IsAlive(x));

        args.BonusDamage += args.ExtraDamage * maelstrom.ExtraDamageMultiplier;
        if (aliveMobsCount <= 0 || !TryComp<DamageableComponent>(user, out var dmg))
            return;

        var heal = args.BaseDamage.GetTotal() * aliveMobsCount * maelstrom.LifestealHealMultiplier;

        _sanguine.LifeSteal((user, dmg), heal);
    }

    [SubscribeLocalEvent]
    private void OnCosmosBlade(Entity<HereticBladeComponent> ent, ref CosmosBladeBonusEvent args)
    {
        args.BonusDamage += args.ExtraDamage;

        var hitEnts = args.HitEntities;

        if (hitEnts.Count == 0)
            return;

        _combo.ComboProgress(args.User, args.PathStage, hitEnts);
    }

    [SubscribeLocalEvent]
    private void OnWoundingBonus(Entity<HereticBladeComponent> ent, ref HereticBladeBonusWoundingEvent args)
    {
        var stage = args.PathStage;
        var defaultPair = new KeyValuePair<int, float>(0, 1f);
        var woundingMultiplier = args.WoundingBonus.LastOrDefault(x => x.Key <= stage, defaultPair).Value;
        if (woundingMultiplier <= 1f)
            return;
        foreach (var dmgType in args.BaseDamage.DamageDict.Keys)
        {
            if (!args.BaseDamage.WoundSeverityMultipliers.TryGetValue(dmgType, out var mult))
                args.BaseDamage.WoundSeverityMultipliers[dmgType] = woundingMultiplier;
            else
                args.BaseDamage.WoundSeverityMultipliers[dmgType] = mult * woundingMultiplier;
        }
    }

    [SubscribeLocalEvent]
    private void OnDamageBonus(Entity<HereticBladeComponent> ent, ref HereticBladeBonusDamageEvent args)
    {
        args.BonusDamage += args.ExtraDamage;
    }

    [SubscribeLocalEvent]
    private void OnGetRange(Entity<HereticBladeComponent> ent, ref GetLightAttackRangeEvent args)
    {
        if (args.Target == null)
            return;

        var user = args.User;

        if (!_heretic.TryGetHereticComponent(user, out var heretic, out _))
            return;

        if (ent.Comp.Path != heretic.CurrentPath)
            return;

        // Required for seeking blade, client weapon code should send attack event regardless of distance
        if (heretic.CurrentPath == HereticPath.Void && heretic.PathStage >= 7)
        {
            if (_net.IsServer)
                return;

            args.Range = 16f;
            args.Cancel = true;
            return;
        }

        if (heretic.CurrentPath != HereticPath.Cosmos)
            return;

        if (HasComp<StarMarkComponent>(args.Target.Value) && heretic.PathStage >= 7)
        {
            if (heretic.Ascended)
            {
                args.Range = Math.Max(args.Range, 3.5f);
                return;
            }

            args.Range = Math.Max(args.Range, 2.5f);
        }

        if (_status.TryEffectsWithComp<StarTouchedStatusEffectComponent>(args.Target.Value, out var effects) &&
            effects.Any(x => x.Comp1.User == user))
            args.Range = Math.Max(args.Range, 3.5f);
    }

    // Void seeking blade
    [SubscribeLocalEvent]
    private void OnSpecial(Entity<HereticBladeComponent> ent, ref LightAttackSpecialInteractionEvent args)
    {
        if (args.Target == null)
            return;

        if (SeekingBladeTeleport(ent, args.User, args.Target.Value, args.Range))
            args.Cancel = true;
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<HereticBladeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        if (SeekingBladeTeleport(ent, args.User, args.Target.Value))
            args.Handled = true;
    }

    private bool SeekingBladeTeleport(Entity<HereticBladeComponent> ent,
        EntityUid user,
        EntityUid target,
        float minRange = 0f,
        float maxRange = 16f)
    {
        var ev = new TeleportAttemptEvent();
        RaiseLocalEvent(user, ref ev);
        if (ev.Cancelled)
            return false;

        if (target == user || ent.Comp.Path != HereticPath.Void ||
            !_heretic.TryGetHereticComponent(user, out var heretic, out _) ||
            !TryComp(user, out CombatModeComponent? combat) ||
            heretic is not { CurrentPath: HereticPath.Void, PathStage: >= 7 } || !HasComp<MobStateComponent>(target) ||
            !TryComp(ent, out MeleeWeaponComponent? melee) || melee.NextAttack > _timing.CurTime)
            return false;

        var xform = Transform(user);
        var targetXform = Transform(target);

        if (xform.MapID != targetXform.MapID)
            return false;

        var coords = _xform.GetWorldPosition(xform);
        var targetCoords = _xform.GetWorldPosition(targetXform);

        var dir = targetCoords - coords;
        var len = dir.Length();
        if (len >= maxRange || len <= minRange)
            return false;

        var normalized = new Vector2(dir.X / len, dir.Y / len);
        var ray = new CollisionRay(coords,
            normalized,
            (int) (CollisionGroup.Impassable | CollisionGroup.InteractImpassable));
        var result = _physics.IntersectRay(xform.MapID, ray, len, user).FirstOrNull();
        if (result != null && result.Value.HitEntity != target)
            return false;

        var newPos = result?.HitPos ?? targetCoords - normalized * 0.5f;

        _audio.PlayPredicted(ent.Comp.DepartureSound, xform.Coordinates, user);
        _xform.SetWorldPosition(user, newPos);
        var combatMode = _combat.IsInCombatMode(user, combat);
        _combat.SetInCombatMode(user, true, combat);
        if (!_melee.AttemptLightAttack(user, ent.Owner, melee, target))
            melee.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1f / _melee.GetAttackRate(ent, user, melee));
        melee.NextAttack += TimeSpan.FromSeconds(0.5);
        Dirty(ent.Owner, melee);
        _combat.SetInCombatMode(user, combatMode, combat);
        _audio.PlayPredicted(ent.Comp.ArrivalSound, xform.Coordinates, user);
        return true;
    }

    public void ApplySpecialEffect(EntityUid performer, EntityUid target, Entity<HereticBladeComponent> blade)
    {
        int? stage = TryComp(performer, out HereticBladeUserBonusDamageComponent? bonus) && bonus.ApplyBladeEffects
            ? 7
            : null;
        if (_heretic.TryGetHereticComponent(performer, out var hereticComp, out _))
            stage = hereticComp.PathStage;

        if (stage == null)
            return;

        var defaultPair = new KeyValuePair<int, float>(0, 1f);
        var prob = blade.Comp.Probabilities.LastOrDefault(x => x.Key <= stage, defaultPair).Value;
        if (prob <= 0f)
            return;

        if (blade.Comp.Effects is not { } effects)
            return;

        foreach (var effect in effects)
        {
            _effects.TryApplyEffect(target, effect, effect.ScaleProbability ? prob : 1f, performer);
        }
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<ActionsContainerComponent> ent, ref HereticBladeBreakFailOverrideEvent args)
    {
        foreach (var action in ent.Comp.Container.ContainedEntities)
        {
            if (!_limitedQuery.TryComp(action, out var limited) || limited.LastCharges >= limited.MaxCharges)
                continue;

            if (!_bladeBreakRechargeQuery.TryComp(action, out var rechargeComp))
                continue;

            _charges.AddCharges((action, limited), rechargeComp.ChargeGain);
            _popup.PopupEntity(Loc.GetString("heretic-blade-break-spell-recharge-message", ("spell", action)),
                args.User,
                args.User);
            args.ShouldShatter = true;
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<HereticBladeComponent> ent, ref UseInHandEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out var mind))
            return;

        if (!heretic.CanBreakBlade)
        {
            var overrideEv = new HereticBladeBreakFailOverrideEvent(args.User);
            RaiseLocalEvent(mind, ref overrideEv);
            if (overrideEv.ShouldShatter)
                ShatterBlade(ent, args.User);
            else
                _popup.PopupEntity(Loc.GetString("heretic-blade-break-fail-message"), args.User, args.User);
            return;
        }

        if (!TryComp<RandomTeleportComponent>(ent, out var rtp))
            return;

        var ev = new TeleportAttemptEvent();
        RaiseLocalEvent(args.User, ref ev);
        if (ev.Cancelled)
            return;

        _teleport.RandomTeleport(args.User, rtp, sound: false, user: args.User);
        ShatterBlade(ent, args.User);
        _popup.PopupEntity(Loc.GetString("heretic-blade-use"), args.User, args.User);
        args.Handled = true;
    }

    private void ShatterBlade(Entity<HereticBladeComponent> ent, EntityUid user)
    {
        PredictedQueueDel(ent);
        _audio.PlayPredicted(ent.Comp.ShatterSound, user, user);
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<HereticBladeComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<RandomTeleportComponent>(ent))
            return;

        if (!_heretic.TryGetHereticComponent(args.Examiner, out var heretic, out _) || !heretic.CanBreakBlade)
            return;

        args.PushMarkup(Loc.GetString("heretic-blade-examine"));
    }

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<HereticBladeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        ApplyBladeHitEffects(ent, args.User, args.HitEntities, out var heretic);
        ApplyBladeBonuses(ent, args.User, heretic, args.BaseDamage, args.BonusDamage, (List<EntityUid>) args.HitEntities);
    }

    private void OnBeforeThrowDamage(Entity<HereticBladeComponent> ent, ref BeforeDamageOtherOnHitEvent args)
    {
        if (args.Cancelled || args.User is not { } user)
            return;

        _heretic.TryGetHereticComponent(user, out var heretic, out _);

        if (heretic == null && !HasComp<GhoulComponent>(user))
            return;

        ApplyBladeBonuses(ent, user, heretic, args.BaseDamage, args.BonusDamage, new() { args.Target });
    }

    [SubscribeLocalEvent]
    private void OnThrowHit(Entity<HereticBladeComponent> ent, ref ThrowDoHitEvent args)
    {
        if (args.Component.Thrower is { } user && HasComp<DamageOtherOnHitComponent>(ent))
            ApplyBladeHitEffects(ent, user, new List<EntityUid>() { args.Target }, out _);
    }

    private void ApplyBladeHitEffects(Entity<HereticBladeComponent> ent,
        EntityUid user,
        IReadOnlyList<EntityUid> targets,
        out HereticComponent? heretic)
    {
        _heretic.TryGetHereticComponent(user, out heretic, out _);

        if (ent.Comp.Path == null)
            return;

        if (heretic == null || ent.Comp.Path != heretic.CurrentPath)
            return;

        foreach (var hit in targets)
        {
            if (hit == user)
                continue;

            if (TryComp<HereticCombatMarkComponent>(hit, out var mark))
                _combatMark.ApplyMarkEffect(hit, mark, user);

            if (heretic.PathStage >= 7)
                ApplySpecialEffect(user, hit, ent);
        }

    }

    private void ApplyBladeBonuses(Entity<HereticBladeComponent> ent,
        EntityUid user,
        HereticComponent? heretic,
        DamageSpecifier baseDamage,
        DamageSpecifier bonusDamage,
        List<EntityUid> targets)
    {
        if (TryComp(user, out HereticBladeUserBonusDamageComponent? bonus) &&
            (bonus.Path == null || bonus.Path == ent.Comp.Path))
        {
            foreach (var key in baseDamage.DamageDict.Keys)
            {
                baseDamage.DamageDict[key] *= bonus.BonusMultiplier;
            }

            if (heretic == null)
            {
                foreach (var hit in targets)
                {
                    ApplySpecialEffect(user, hit, ent);
                }
            }
        }

        if (heretic?.PathStage >= 7 && ent.Comp.BonusEvent is { } ev)
        {
            ev.User = user;
            ev.BonusDamage = bonusDamage;
            ev.BaseDamage = baseDamage;
            ev.HitEntities = targets;
            ev.PathStage = heretic.PathStage;
            RaiseLocalEvent(ent, (object) ev);
        }
    }
}
