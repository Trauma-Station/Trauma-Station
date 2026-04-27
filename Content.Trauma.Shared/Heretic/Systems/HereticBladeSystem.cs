// SPDX-License-Identifier: AGPL-3.0-or-later


using System.Linq;
using System.Numerics;
using Content.Goobstation.Common.BlockTeleport;
using Content.Goobstation.Common.Physics;
using Content.Goobstation.Common.Weapons;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Teleportation.Systems;
using Content.Trauma.Shared.Wizard.SanguineStrike;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed class HereticBladeSystem : EntitySystem
{
    [Dependency] private readonly CosmosComboSystem _combo = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RandomTeleportSystem _teleport = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedHereticCombatMarkSystem _combatMark = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSanguineStrikeSystem _sanguine = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticBladeComponent, UseInHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticBladeComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HereticBladeComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<HereticBladeComponent, GetLightAttackRangeEvent>(OnGetRange);
        SubscribeLocalEvent<HereticBladeComponent, LightAttackSpecialInteractionEvent>(OnSpecial);
        SubscribeLocalEvent<HereticBladeComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HereticBladeComponent, HereticBladeBonusDamageEvent>(OnDamageBonus);
        SubscribeLocalEvent<HereticBladeComponent, HereticBladeBonusWoundingEvent>(OnWoundingBonus);
        SubscribeLocalEvent<HereticBladeComponent, CosmosBladeBonusEvent>(OnCosmosBlade);
        SubscribeLocalEvent<HereticBladeComponent, BladeBladeBonusEvent>(OnBladeBlade);
    }

    private void OnBladeBlade(Entity<HereticBladeComponent> ent, ref BladeBladeBonusEvent args)
    {
        args.Args.BonusDamage += args.BonusDamage;

        var user = args.Args.User;

        if (!TryComp(user, out SilverMaelstromComponent? maelstrom))
            return;

        var aliveMobsCount = args.Args.HitEntities.Count(x => x != user && _mobState.IsAlive(x));

        args.BonusDamage += args.Args.BaseDamage * maelstrom.ExtraDamageMultiplier;
        if (aliveMobsCount <= 0 || !TryComp<DamageableComponent>(user, out var dmg))
            return;

        var heal = args.Args.BaseDamage.GetTotal() * aliveMobsCount * maelstrom.LifestealHealMultiplier;

        _sanguine.LifeSteal((user, dmg), heal);
    }

    private void OnCosmosBlade(Entity<HereticBladeComponent> ent, ref CosmosBladeBonusEvent args)
    {
        args.Args.BonusDamage += args.BonusDamage;

        var hitEnts = args.Args.HitEntities;

        if (hitEnts.Count == 0)
            return;

        _combo.ComboProgress(args.Args.User, args.PathStage, hitEnts);
    }

    private void OnWoundingBonus(Entity<HereticBladeComponent> ent, ref HereticBladeBonusWoundingEvent args)
    {
        var stage = args.PathStage;
        var defaultPair = new KeyValuePair<int, float>(0, 1f);
        var woundingMultiplier = args.WoundingBonus.LastOrDefault(x => x.Key <= stage, defaultPair).Value;
        if (woundingMultiplier <= 1f)
            return;
        foreach (var dmgType in args.Args.BaseDamage.DamageDict.Keys)
        {
            if (!args.Args.BaseDamage.WoundSeverityMultipliers.TryGetValue(dmgType, out var mult))
                args.Args.BaseDamage.WoundSeverityMultipliers[dmgType] = woundingMultiplier;
            else
                args.Args.BaseDamage.WoundSeverityMultipliers[dmgType] = mult * woundingMultiplier;
        }
    }

    private void OnDamageBonus(Entity<HereticBladeComponent> ent, ref HereticBladeBonusDamageEvent args)
    {
        args.Args.BonusDamage += args.BonusDamage;
    }

    private void OnGetRange(Entity<HereticBladeComponent> ent, ref GetLightAttackRangeEvent args)
    {
        if (args.Target == null)
            return;

        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        if (ent.Comp.Path != heretic.CurrentPath || heretic.PathStage < 7)
            return;

        // Required for seeking blade, client weapon code should send attack event regardless of distance
        if (heretic.CurrentPath == HereticPath.Void)
        {
            if (_net.IsServer)
                return;

            args.Range = 16f;
            args.Cancel = true;
            return;
        }

        if (heretic.CurrentPath != HereticPath.Cosmos)
            return;

        if (HasComp<Trauma.Shared.Heretic.Components.PathSpecific.Cosmos.StarMarkComponent>(args.Target.Value))
        {
            if (heretic.Ascended)
            {
                args.Range = Math.Max(args.Range, 3.5f);
                return;
            }

            args.Range = Math.Max(args.Range, 2.5f);
        }

        var netEnt = GetNetEntity(args.User);
        var id = SharedStarTouchSystem.StarTouchBeamDataId;

        if (TryComp(args.Target.Value, out ComplexJointVisualsComponent? joint) &&
            joint.Data.Any(kvp => kvp.Key == netEnt && kvp.Value.Id == id))
            args.Range = Math.Max(args.Range, 3.5f);
    }

    // Void seeking blade

    private void OnSpecial(Entity<HereticBladeComponent> ent, ref LightAttackSpecialInteractionEvent args)
    {
        if (args.Target == null)
            return;

        if (SeekingBladeTeleport(ent, args.User, args.Target.Value, args.Range))
            args.Cancel = true;
    }

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

    private void OnInteract(Entity<HereticBladeComponent> ent, ref UseInHandEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        if (!heretic.CanBreakBlade)
        {
            _popup.PopupClient(Loc.GetString("heretic-blade-break-fail-message"), args.User, args.User);
            return;
        }

        if (!TryComp<RandomTeleportComponent>(ent, out var rtp))
            return;

        var ev = new TeleportAttemptEvent();
        RaiseLocalEvent(args.User, ref ev);
        if (ev.Cancelled)
            return;

        _teleport.RandomTeleport(args.User, rtp, sound: false, user: args.User);
        PredictedQueueDel(ent);
        _audio.PlayPredicted(ent.Comp.ShatterSound, args.User, args.User);
        _popup.PopupClient(Loc.GetString("heretic-blade-use"), args.User, args.User);
        args.Handled = true;
    }

    private void OnExamine(Entity<HereticBladeComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<RandomTeleportComponent>(ent))
            return;

        if (!_heretic.TryGetHereticComponent(args.Examiner, out var heretic, out _) || !heretic.CanBreakBlade)
            return;

        args.PushMarkup(Loc.GetString("heretic-blade-examine"));
    }

    private void OnMeleeHit(Entity<HereticBladeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || ent.Comp.Path == null)
            return;

        _heretic.TryGetHereticComponent(args.User, out var hereticComp, out _);

        if (TryComp(args.User, out HereticBladeUserBonusDamageComponent? bonus) &&
            (bonus.Path == null || bonus.Path == ent.Comp.Path))
        {
            args.BonusDamage += args.BaseDamage * bonus.BonusMultiplier;
            if (hereticComp == null)
            {
                foreach (var hit in args.HitEntities)
                {
                    ApplySpecialEffect(args.User, hit, ent);
                }
            }
        }

        if (hereticComp == null || ent.Comp.Path != hereticComp.CurrentPath)
            return;

        if (hereticComp.PathStage >= 7 && ent.Comp.BonusEvent is { } ev)
        {
            ev.Args = args;
            ev.PathStage = hereticComp.PathStage;
            RaiseLocalEvent(ent, (object) ev);
        }

        foreach (var hit in args.HitEntities)
        {
            if (hit == args.User)
                continue;

            if (TryComp<HereticCombatMarkComponent>(hit, out var mark))
                _combatMark.ApplyMarkEffect(hit, mark, args.User);

            if (hereticComp.PathStage >= 7)
                ApplySpecialEffect(args.User, hit, ent);
        }
    }
}
