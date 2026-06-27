// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Wizard.Projectiles;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Weapons.Reflect;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Parry;

/// <summary>
/// This handles logic for <see cref="ParryComponent" />.
/// </summary>
public sealed partial class ParrySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private AlertsSystem _alert = default!;

    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;
    [Dependency] private EntityQuery<ReflectiveComponent> _reflectiveQuery = default!;

    private static readonly EntProtoId MeleeKnowledge = "MeleeKnowledge";
    private static readonly TimeSpan ExhaustionRegenDelay = TimeSpan.FromSeconds(1);
    private TimeSpan _nextRegen = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParryComponent, HeldRelayedEvent<ProjectileReflectAttemptEvent>>(OnReflectProjectile);
        SubscribeLocalEvent<ParryComponent, HeldRelayedEvent<HitScanReflectAttemptEvent>>(OnReflectHitscan);
        SubscribeLocalEvent<ParryComponent, HeldRelayedEvent<BeforeHarmfulActionEvent>>(OnParry);
        SubscribeLocalEvent<ParryComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<ParryExhaustionComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;

        if (now < _nextRegen)
            return;

        _nextRegen = now + ExhaustionRegenDelay;

        var query = EntityQueryEnumerator<ParryExhaustionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Exhaustion <= 0f || now < comp.ExhaustionRegenTimer)
                continue;

            var level = GetSkillLevel(uid);
            var regen = comp.ExhaustionRegenRate * _knowledge.SharpCurve(level, 50 );

            comp.Exhaustion = Math.Clamp(comp.Exhaustion - regen, 0f, 1f);
            UpdateAlert((uid, comp));
            Dirty(uid, comp);
        }
    }

    private void OnShutdown(Entity<ParryExhaustionComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _alert.ClearAlert(ent.Owner, ent.Comp.Alert);
    }

    private void OnReflectProjectile(Entity<ParryComponent> ent,
        ref HeldRelayedEvent<ProjectileReflectAttemptEvent> args)
    {
        if (args.Args.Cancelled)
            return;
        if (TryReflectProjectile(ent, args.Args.Target, args.Args.ProjUid))
            args.Args.Cancelled = true;
    }

    private void OnReflectHitscan(Entity<ParryComponent> ent, ref HeldRelayedEvent<HitScanReflectAttemptEvent> args)
    {
        if (args.Args.Reflected)
            return;

        if (!TryReflectHitscan(ent,
                args.Args.Target,
                args.Args.Shooter,
                args.Args.SourceItem,
                args.Args.Direction,
                args.Args.Reflective,
                out var dir))
            return;

        args.Args.Direction = dir.Value;
        args.Args.Reflected = true;
    }

    private void OnParry(Entity<ParryComponent> ent, ref HeldRelayedEvent<BeforeHarmfulActionEvent> args)
    {
        if (args.Args.Cancelled || args.Args.Type != HarmfulActionType.Harm)
            return;

        if (TryParry(ent, args.Args.Target, args.Args.User))
            args.Args.Cancelled = true;
    }

    public bool TryReflectProjectile(Entity<ParryComponent> reflector,
        EntityUid user,
        Entity<ProjectileComponent?> projectile)
    {
        if (!_reflectiveQuery.TryComp(projectile, out var reflective)
            || !_physicsQuery.TryComp(projectile, out var physics)
            || (reflector.Comp.Reflects & reflective.Reflective) == 0x0 // Check if the reflective types match
            || !_toggle.IsActivated(reflector.Owner) // If the item can be toggled (e.g. esword) check if it's on
            || !CheckKnowledge(user, reflector.Comp.ReflectMinSkill)
            || !CheckAndUpdateExhaustion(user, reflector, true))
            return false;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(reflector));
        var angle = reflector.Comp.ReflectSpread;
        var rotation = rand.NextAngle(-angle / 2, angle / 2).Opposite();
        var existingVelocity = _physics.GetMapLinearVelocity(projectile, component: physics);
        var relativeVelocity = existingVelocity - _physics.GetMapLinearVelocity(user);
        var newVelocity = rotation.RotateVec(relativeVelocity);

        // Have the velocity in world terms above so need to convert it back to local.
        var difference = newVelocity - existingVelocity;

        _physics.SetLinearVelocity(projectile, physics.LinearVelocity + difference, body: physics);

        var locRot = Transform(projectile).LocalRotation;
        var newRot = rotation.RotateVec(locRot.ToVec());
        _transform.SetLocalRotation(projectile, newRot.ToAngle());

        RemCompDeferred<HomingProjectileComponent>(projectile);

        EntityUid? shooter = null;
        if (Resolve(projectile, ref projectile.Comp, false))
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{user} reflected {projectile} from {projectile.Comp.Weapon} shot by {projectile.Comp.Shooter}");

            shooter = projectile.Comp.OriginalShooter;
            projectile.Comp.Shooter = user;
            projectile.Comp.Weapon = user;
            Dirty(projectile, projectile.Comp);
        }
        else
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{user} reflected {projectile}");
        }

        PlayAudioAndPopup(reflector.Comp.SoundOnReflect, user, shooter);

        return true;
    }

    public bool TryReflectHitscan(
        Entity<ParryComponent> reflector,
        EntityUid user,
        EntityUid? shooter,
        EntityUid shotSource,
        Vector2 direction,
        ReflectType hitscanReflectType,
        [NotNullWhen(true)] out Vector2? newDirection)
    {
        if ((reflector.Comp.Reflects & hitscanReflectType) == 0x0 // Check if the reflective types match
            || !_toggle.IsActivated(reflector.Owner) // If the item can be toggled (e.g. esword) check if it's on
            || !CheckKnowledge(user, reflector.Comp.ReflectMinSkill)
            || !CheckAndUpdateExhaustion(user, reflector, true))
        {
            newDirection = null;
            return false;
        }

        PlayAudioAndPopup(reflector.Comp.SoundOnReflect, user, shooter);

        var angle = reflector.Comp.ReflectSpread;
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(reflector));
        var spread = rand.NextAngle(-angle / 2, angle / 2);
        newDirection = -spread.RotateVec(direction);

        if (shooter != null)
        {
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{user} reflected hitscan from {shotSource} shot by {shooter.Value}");
        }
        else
        {
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{user} reflected hitscan from {shotSource}");
        }

        return true;
    }

    private bool TryParry(Entity<ParryComponent> reflector, EntityUid user, EntityUid attacker)
    {
        if (!_toggle.IsActivated(reflector.Owner)
            || !CheckKnowledge(user, reflector.Comp.ReflectMinSkill)
            || !CheckAndUpdateExhaustion(user, reflector, false)
            || user == attacker) // Me when I try to kill myself, but I parry the hit
            return false;

        PlayAudioAndPopup(reflector.Comp.SoundOnParry, user, attacker);

        _adminLogger.Add(LogType.MeleeHit, LogImpact.Medium, $"{user} parried a melee strike from {attacker}");

        return true;
    }

    /// <summary>
    /// Check if the entity has sufficient knowledge to parry/reflect
    /// </summary>
    private bool CheckKnowledge(EntityUid user, int minLevel)
    {
        return GetSkillLevel(user) >= minLevel;
    }

    private int GetSkillLevel(EntityUid user)
    {
        return _proto.Resolve(MeleeKnowledge, out var skillProto)
               && _knowledge.GetContainer(user) is { } brain
               && _knowledge.GetKnowledge(brain, skillProto) is { } skill
            ? skill.Comp.NetLevel
            : 0;
    }

    /// <summary>
    /// Check if the entity is too exhausted to parry/reflect and add an appropriate amount of exhaustion
    /// </summary>
    private bool CheckAndUpdateExhaustion(EntityUid user, Entity<ParryComponent> item, bool isReflect)
    {
        var comp = EnsureComp<ParryExhaustionComponent>(user);

        var maxExh = isReflect ? comp.MaxReflectExhaustion : comp.MaxParryExhaustion;
        var cost = isReflect ? item.Comp.ReflectExhaustionCost : item.Comp.ParryExhaustionCost;
        var newExh = comp.Exhaustion + cost;

        if (comp.Exhaustion >= maxExh || newExh > 1f)
            return false;

        comp.Exhaustion = newExh;
        comp.ExhaustionRegenTimer = _timing.CurTime + comp.ExhaustionRegenDelay;
        UpdateAlert((user, comp));
        Dirty(user, comp);
        return true;
    }

    private void OnExamine(Entity<ParryComponent> ent, ref ExaminedEvent args)
    {
        AppendParryExamine(ent, ref args);
        AppendReflectExamine(ent, ref args);
    }

    private void AppendParryExamine(Entity<ParryComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ParryExhaustionCost > 1f)
            return;

        var level = GetSkillLevel(args.Examiner);
        if (level < ent.Comp.ParryMinSkill)
        {
            args.PushMarkup(Loc.GetString("parry-component-examine-lowskill"));
            return;
        }

        var comp = EnsureComp<ParryExhaustionComponent>(args.Examiner);

        if (ent.Comp.ParryExhaustionCost <= 0f)
        {
            args.PushMarkup("parry-component-examine-unlimited");
            return;
        }

        var value = (int) MathF.Ceiling(Math.Clamp(comp.MaxParryExhaustion, 0f, 1f) / ent.Comp.ParryExhaustionCost);
        args.PushMarkup(Loc.GetString("parry-component-examine", ("value", value)));
    }

    private void AppendReflectExamine(Entity<ParryComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Reflects == ReflectType.None ||
            ent.Comp.ReflectExhaustionCost > 1f)
            return;

        var compTypes = ent.Comp.Reflects.ToString().Split(", ");
        List<string> typeList = new(compTypes.Length);
        foreach (var t in compTypes)
        {
            typeList.Add(Loc.GetString(("reflect-component-" + t).ToLower()));
        }

        var msg = ContentLocalizationManager.FormatListToOr(typeList);

        var level = GetSkillLevel(args.Examiner);
        if (level < ent.Comp.ReflectMinSkill)
        {
            args.PushMarkup(Loc.GetString("parry-component-examine-reflect-lowskill", ("type", msg)));
            return;
        }

        var comp = EnsureComp<ParryExhaustionComponent>(args.Examiner);

        if (ent.Comp.ParryExhaustionCost <= 0f)
        {
            args.PushMarkup(Loc.GetString("parry-component-examine-reflect-unlimited", ("type", msg)));
            return;
        }

        var value = (int) MathF.Ceiling(Math.Clamp(comp.MaxReflectExhaustion, 0f, 1f) / ent.Comp.ReflectExhaustionCost);
        args.PushMarkup(Loc.GetString("parry-component-examine-reflect", ("value", value), ("type", msg)));
    }

    private void PlayAudioAndPopup(SoundSpecifier? sound, EntityUid user, EntityUid? shooter)
    {
        _popup.PopupPredicted(Loc.GetString("reflect-shot"), user, shooter);
        _audio.PlayPredicted(sound, user, shooter);
    }

    private void UpdateAlert(Entity<ParryExhaustionComponent> ent)
    {
        var max = _alert.GetMaxSeverity(ent.Comp.Alert);
        var min = _alert.GetMinSeverity(ent.Comp.Alert);
        var severity = (short) MathF.Round(ent.Comp.Exhaustion * ent.Comp.AlertSeverityMultiplier);
        if (severity < min || severity > max)
            _alert.ClearAlert(ent.Owner, ent.Comp.Alert);
        else
            _alert.UpdateAlert(ent.Owner, ent.Comp.Alert, severity);
    }
}
