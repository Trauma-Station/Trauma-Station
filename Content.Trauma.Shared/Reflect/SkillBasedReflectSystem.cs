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
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Weapons.Reflect;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Reflect;

/// <summary>
/// This handles logic for <see cref="SkillBasedReflectComponent" />.
/// </summary>
public sealed class SkillBasedReflectSystem : EntitySystem
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkillBasedReflectComponent, HeldRelayedEvent<ProjectileReflectAttemptEvent>>(OnReflectUserCollide);
        SubscribeLocalEvent<SkillBasedReflectComponent, HeldRelayedEvent<HitScanReflectAttemptEvent>>(OnReflectUserHitscan);

        SubscribeLocalEvent<SkillBasedReflectComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SkillBasedReflectExhaustionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime > comp.ExhaustionRegenTimer) continue;
            comp.Exhaustion -= comp.ExhaustionRegenRate * frameTime;
            if (comp.Exhaustion <= 0) RemCompDeferred<SkillBasedReflectExhaustionComponent>(uid);
        }
    }

    private void OnReflectUserCollide(Entity<SkillBasedReflectComponent> ent, ref HeldRelayedEvent<ProjectileReflectAttemptEvent> args)
    {
        if (args.Args.Cancelled)
            return;
        if (TryReflectProjectile(ent, ent.Owner, args.Args.ProjUid))
            args.Args.Cancelled = true;
    }

    private void OnReflectUserHitscan(Entity<SkillBasedReflectComponent> ent, ref HeldRelayedEvent<HitScanReflectAttemptEvent> args)
    {
        if (args.Args.Reflected)
            return;

        if (TryReflectHitscan(ent, ent.Owner, args.Args.Shooter, args.Args.SourceItem, args.Args.Direction, args.Args.Reflective, out var dir))
        {
            args.Args.Direction = dir.Value;
            args.Args.Reflected = true;
        }
    }

    public bool TryReflectProjectile(Entity<SkillBasedReflectComponent> reflector, EntityUid user, Entity<ProjectileComponent?> projectile)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(reflector));
        if (!TryComp<ReflectiveComponent>(projectile, out var reflective)
            || (reflector.Comp.Reflects & reflective.Reflective) == 0x0
            || !_toggle.IsActivated(reflector.Owner)
            || !TryComp<PhysicsComponent>(projectile, out var physics)
            || !rand.Prob(CalculateReflectChance(reflector, user)))
        {
            return false;
        }

        var angle = CalculateReflectAngle(reflector, user);
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

        _popup.PopupClient(Loc.GetString("reflect-shot"), user, user);
        _audio.PlayLocal(reflector.Comp.SoundOnReflect, user, null);

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
        Entity<SkillBasedReflectComponent> reflector,
        EntityUid user,
        EntityUid? shooter,
        EntityUid shotSource,
        Vector2 direction,
        ReflectType hitscanReflectType,
        [NotNullWhen(true)] out Vector2? newDirection)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(reflector));
        if ((reflector.Comp.Reflects & hitscanReflectType) == 0x0
            || !_toggle.IsActivated(reflector.Owner)
            || !rand.Prob(CalculateReflectChance(reflector, user)))
        {
            newDirection = null;
            return false;
        }

        _popup.PopupClient(Loc.GetString("reflect-shot"), user, user);
        _audio.PlayLocal(reflector.Comp.SoundOnReflect, user, null);

        var angle = CalculateReflectAngle(reflector, user);
        var spread = rand.NextAngle(-angle / 2, angle / 2);
        newDirection = -spread.RotateVec(direction);

        if (shooter != null)
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected hitscan from {ToPrettyString(shotSource)} shot by {ToPrettyString(shooter.Value)}");
        else
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected hitscan from {ToPrettyString(shotSource)}");

        return true;
    }
    #region Calculations

    /// <summary>
    /// The chance for a given entity to reflect a shot with a given item, based on current exhaustion and melee skill.
    /// </summary>
    private float CalculateReflectChance(Entity<SkillBasedReflectComponent> item, EntityUid user)
    {
        if (!_proto.Resolve(item.Comp.RequiredSkill, out var skillProto))
            return 0f;
        if (_knowledge.GetContainer(user) is not { } brain)
        {
            Log.Error("fuck");
            return 0f;
        }
        if (_knowledge.GetKnowledge(brain, skillProto) is not { } meleeSkill)
        {
            Log.Error("fuck2");
            return 0f;
        }
        if (meleeSkill.Comp.Level < item.Comp.MinSkill)
        {
            Log.Error("fuck3");
            return 0f;
        }

        EnsureComp<SkillBasedReflectExhaustionComponent>(user, out var exhaustionComp);

        var chance = item.Comp.BaseProb * (1f - exhaustionComp.Exhaustion) + (float) (Math.Pow(meleeSkill.Comp.Level / 100f, 2f) + 0.5f);

        exhaustionComp.ExhaustionRegenTimer = _timing.CurTime + exhaustionComp.ExhaustionRegenDelay;
        exhaustionComp.Exhaustion += item.Comp.ExhaustionPerReflectAttempt;
        Dirty(user, exhaustionComp);

        Log.Info("Chance is {0}", chance);

        return Math.Clamp(chance, 0f, 1f);
    }

    private Angle CalculateReflectAngle(Entity<SkillBasedReflectComponent> item, EntityUid user)
    {
        if (!_proto.Resolve(item.Comp.RequiredSkill, out var skillProto))
            return 0f;
        if (_knowledge.GetContainer(user) is not { } brain
        || _knowledge.GetKnowledge(brain, skillProto) is not { } meleeSkill
        || meleeSkill.Comp.Level < item.Comp.MinSkill)
            return item.Comp.Spread;

        EnsureComp<SkillBasedReflectExhaustionComponent>(user, out var exhaustionComp);

        return item.Comp.Spread * (0.2f + exhaustionComp.Exhaustion) * (1.3 - Math.Pow(meleeSkill.Comp.Level / 100f, 2f));
    }
    #endregion

    #region Examine
    // TODO: ideally this should approximate how many times you can reflect in a quick succession, but I can't think of a way that isn't dogshit
    private void OnExamine(Entity<SkillBasedReflectComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Reflects == ReflectType.None)
            return;

        var compTypes = ent.Comp.Reflects.ToString().Split(", ");

        List<string> typeList = new(compTypes.Length);

        for (var i = 0; i < compTypes.Length; i++)
        {
            var type = Loc.GetString(("reflect-component-" + compTypes[i]).ToLower());
            typeList.Add(type);
        }

        var msg = ContentLocalizationManager.FormatList(typeList);

        args.PushMarkup(Loc.GetString("reflect-skill-component-examine", ("type", msg)));
    }
    #endregion
}
