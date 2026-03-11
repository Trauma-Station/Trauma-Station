// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Wizard.Projectiles;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Administration.Logs;
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
using Content.Trauma.Common.Parry;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Parry;

/// <summary>
/// This handles logic for <see cref="ParryComponent" />.
/// </summary>
public sealed class ParrySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private EntityQuery<ReflectiveComponent> _reflectiveQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParryComponent, HeldRelayedEvent<ProjectileReflectAttemptEvent>>(OnReflectProjectile);
        SubscribeLocalEvent<ParryComponent, HeldRelayedEvent<HitScanReflectAttemptEvent>>(OnReflectHitscan);
        SubscribeLocalEvent<ParryComponent, HeldRelayedEvent<ParryAttemptEvent>>(OnParry);

        SubscribeLocalEvent<ParryComponent, ExaminedEvent>(OnExamine);

        _reflectiveQuery = GetEntityQuery<ReflectiveComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ParryExhaustionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.ExhaustionRegenTimer)
                continue;

            comp.Exhaustion -= comp.ExhaustionRegenRate * frameTime;
            if (comp.Exhaustion <= 0) RemCompDeferred<ParryExhaustionComponent>(uid);
        }
    }

    private void OnReflectProjectile(Entity<ParryComponent> ent, ref HeldRelayedEvent<ProjectileReflectAttemptEvent> args)
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

        if (TryReflectHitscan(ent, args.Args.Target, args.Args.Shooter, args.Args.SourceItem, args.Args.Direction, args.Args.Reflective, out var dir))
        {
            args.Args.Direction = dir.Value;
            args.Args.Reflected = true;
        }
    }

    private void OnParry(Entity<ParryComponent> ent, ref HeldRelayedEvent<ParryAttemptEvent> args)
    {
        if (args.Args.Parried)
            return;
        if (TryParry(ent, args.Args.Target, args.Args.User))
            args.Args.Parried = true;
    }

    public bool TryReflectProjectile(Entity<ParryComponent> reflector, EntityUid user, Entity<ProjectileComponent?> projectile)
    {
        if (!_reflectiveQuery.TryComp(projectile, out var reflective)
        || !_physicsQuery.TryComp(projectile, out var physics)
        || (reflector.Comp.Reflects & reflective.Reflective) == 0x0 // Check if the reflective types match
        || !_toggle.IsActivated(reflector.Owner) // If the item can be toggled (e.g. esword) check if it's on
        || !CheckKnowledge(user, reflector.Comp.RequiredSkill, reflector.Comp.ReflectMinSkill)
        || !CheckAndUpdateExhaustion(user, reflector))
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

        if (_timing.IsFirstTimePredicted)
        {
            _popup.PopupClient(Loc.GetString("reflect-shot"), user, _player.LocalEntity);
            _audio.PlayLocal(reflector.Comp.SoundOnReflect, user, _player.LocalEntity);
        }

        if (Resolve(projectile, ref projectile.Comp, false))
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected {ToPrettyString(projectile)} from {ToPrettyString(projectile.Comp.Weapon)} shot by {projectile.Comp.Shooter}");

            projectile.Comp.Shooter = user;
            projectile.Comp.Weapon = user;
            Dirty(projectile, projectile.Comp);
        }
        else
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected {ToPrettyString(projectile)}");
        }

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
        || !CheckKnowledge(user, reflector.Comp.RequiredSkill, reflector.Comp.ReflectMinSkill)
        || !CheckAndUpdateExhaustion(user, reflector))
        {
            newDirection = null;
            return false;
        }

        if (_timing.IsFirstTimePredicted)
        {
            _popup.PopupClient(Loc.GetString("reflect-shot"), user, _player.LocalEntity);
            _audio.PlayLocal(reflector.Comp.SoundOnReflect, user, _player.LocalEntity);
        }

        var angle = reflector.Comp.ReflectSpread;
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(reflector));
        var spread = rand.NextAngle(-angle / 2, angle / 2);
        newDirection = -spread.RotateVec(direction);

        if (shooter != null)
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected hitscan from {ToPrettyString(shotSource)} shot by {ToPrettyString(shooter.Value)}");
        else
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected hitscan from {ToPrettyString(shotSource)}");

        return true;
    }

    private bool TryParry(Entity<ParryComponent> reflector, EntityUid user, EntityUid attacker)
    {
        if (!_toggle.IsActivated(reflector.Owner)
        || !CheckKnowledge(user, reflector.Comp.RequiredSkill, reflector.Comp.ReflectMinSkill)
        || !CheckAndUpdateExhaustion(user, reflector, useParryValues: true))
            return false;

        if (_timing.IsFirstTimePredicted)
        {
            _popup.PopupClient(Loc.GetString("reflect-shot"), user, _player.LocalEntity); // Same popup who cares anyway
            _audio.PlayLocal(reflector.Comp.SoundOnParry, user, _player.LocalEntity);
        }

        _adminLogger.Add(LogType.MeleeHit, LogImpact.Medium, $"{ToPrettyString(user)} parried a melee strike from {ToPrettyString(attacker)}");

        return true;
    }

    /// <summary>
    /// Check if the entity has sufficient knowledge to parry/reflect
    /// </summary>
    private bool CheckKnowledge(EntityUid user, EntProtoId knowledge, int minLevel)
    {
        return _proto.Resolve(knowledge, out var skillProto)
        && _knowledge.GetContainer(user) is { } brain
        && _knowledge.GetKnowledge(brain, skillProto) is { } skill
        && skill.Comp.Level >= minLevel;
    }

    /// <summary>
    /// Check if the entity is too exhausted to parry/reflect and add an appropriate amount of exhaustion
    /// </summary>
    private bool CheckAndUpdateExhaustion(EntityUid user, Entity<ParryComponent> item, bool useParryValues = false)
    {
        var exhComp = EnsureComp<ParryExhaustionComponent>(user);

        if (!_proto.Resolve(item.Comp.RequiredSkill, out var skillProto)
        || _knowledge.GetContainer(user) is not { } brain
        || _knowledge.GetKnowledge(brain, skillProto) is not { } skill)
            return false; // Shouldn't ever happen because we check this right after checking knowledge

        var result = exhComp.Exhaustion < 1f;
        var level = Math.Max(skill.Comp.Level, 1); // Evil division by 0
        var exhGain = useParryValues ?
            1f / item.Comp.MaxParries * (100f / level) :
            1f / item.Comp.MaxReflects * (100f / level);
        exhComp.Exhaustion = Math.Min(exhComp.Exhaustion + exhGain, 1f);
        exhComp.ExhaustionRegenTimer = _timing.CurTime + exhComp.ExhaustionRegenDelay;
        Dirty(user, exhComp);
        return result;
    }

    private void OnExamine(Entity<ParryComponent> ent, ref ExaminedEvent args)
    {
        AppendParryExamine(ent, ref args);
        AppendReflectExamine(ent, ref args);
    }

    private void AppendParryExamine(Entity<ParryComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.MaxParries <= 0
        || !_proto.Resolve(ent.Comp.RequiredSkill, out var skillProto)
        || _knowledge.GetContainer(args.Examiner) is not { } brain
        || _knowledge.GetKnowledge(brain, skillProto) is not { } skill)
            return;

        if (skill.Comp.Level < ent.Comp.ParryMinSkill)
        {
            args.PushMarkup(Loc.GetString("parry-component-examine-lowskill"));
            return;
        }
        var value = Math.Ceiling(ent.Comp.MaxParries * (skill.Comp.Level / 100f));
        args.PushMarkup(Loc.GetString("parry-component-examine", ("value", value)));
    }

    private void AppendReflectExamine(Entity<ParryComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Reflects == ReflectType.None
        || ent.Comp.MaxReflects <= 0
        || !_proto.Resolve(ent.Comp.RequiredSkill, out var skillProto)
        || _knowledge.GetContainer(args.Examiner) is not { } brain
        || _knowledge.GetKnowledge(brain, skillProto) is not { } skill)
            return;

        var compTypes = ent.Comp.Reflects.ToString().Split(", ");
        List<string> typeList = new(compTypes.Length);
        for (var i = 0; i < compTypes.Length; i++)
            typeList.Add(Loc.GetString(("reflect-component-" + compTypes[i]).ToLower()));

        var msg = ContentLocalizationManager.FormatListToOr(typeList);

        if (skill.Comp.Level < ent.Comp.ReflectMinSkill)
        {
            args.PushMarkup(Loc.GetString("parry-component-examine-reflect-lowskill", ("type", msg)));
            return;
        }
        var value = Math.Ceiling(ent.Comp.MaxReflects * (skill.Comp.Level / 100f));
        args.PushMarkup(Loc.GetString("parry-component-examine-reflect", ("value", value), ("type", msg)));
    }
}
