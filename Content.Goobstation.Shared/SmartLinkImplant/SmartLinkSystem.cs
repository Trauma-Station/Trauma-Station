using System.Linq;
using Content.Goobstation.Common.Weapons.Ranged;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared._Goobstation.Wizard.Projectiles;
using Content.Shared.Body;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.SmartLinkImplant;

public sealed class SmartLinkSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmartLinkArmComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SmartLinkArmComponent, OrganGotInsertedEvent>(OnAttach);
        SubscribeLocalEvent<SmartLinkArmComponent, OrganGotRemovedEvent>(OnRemove);

        SubscribeLocalEvent<SmartLinkComponent, AmmoShotUserEvent>(OnShot);
    }

    private void OnInit(Entity<SmartLinkArmComponent> ent, ref ComponentInit args) => UpdateComp(ent);

    private void OnAttach(Entity<SmartLinkArmComponent> ent, ref OrganGotInsertedEvent args) => UpdateComp(ent);

    private void OnRemove(Entity<SmartLinkArmComponent> ent, ref OrganGotRemovedEvent args) => UpdateComp(ent);

    private void UpdateComp(Entity<SmartLinkArmComponent> ent)
    {
        if (_body.GetBody(ent.Owner) is not {} body)
            return;

        var arms = 0;
        var linked = 0;
        foreach (var part in _part.GetBodyParts(body, BodyPartType.Arm))
        {
            arms++;
            if (HasComp<SmartLinkArmComponent>(part))
                linked++;
        }
        // all arms must be smart linked, and you need at least 1 arm
        if (linked < 1 || linked < arms)
            RemComp<SmartLinkComponent>(body);
        else
            EnsureComp<SmartLinkComponent>(body);
    }

    private void OnShot(Entity<SmartLinkComponent> ent, ref AmmoShotUserEvent args)
    {
        var (uid, comp) = ent;

        if (!TryComp(args.Gun, out GunComponent? gun) || gun.Target == null)
            return;

        if (gun.Target == Transform(uid).ParentUid)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            if (HasComp<SmartLinkBlacklistComponent>(projectile))
                continue;

            var homing = EnsureComp<DelayedHomingProjectileComponent>(projectile);
            homing.HomingStart = _timing.CurTime + TimeSpan.FromSeconds(0.35f);
            homing.Target = gun.Target.Value;
            Dirty(projectile, homing);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DelayedHomingProjectileComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime < comp.HomingStart)
                continue;

            var homing = EnsureComp<DelayedHomingProjectileComponent>(ent);
            homing.Target = comp.Target;
            RemCompDeferred<DelayedHomingProjectileComponent>(ent);
        }
    }
}
