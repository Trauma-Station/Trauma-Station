// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 SX_7 <sn1.test.preria.2002@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Follower;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared._Goobstation.Heretic.Systems;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared._Shitcode.Heretic.Systems.Abilities;
using Content.Shared.Input;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Goobstation.Shared.Heretic;

[ByRefEvent]
public record struct ProtectiveBladeUsedEvent(Entity<ProtectiveBladeComponent> Used);

public sealed class ProtectiveBladeSystem : EntitySystem
{
    [Dependency] private readonly FollowerSystem _follow = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ReflectSystem _reflect = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public static readonly EntProtoId<ProtectiveBladeComponent> BladePrototype = "HereticProtectiveBlade";
    public static readonly EntProtoId BladeProjecilePrototype = "HereticProtectiveBladeProjectile";
    public static readonly SoundSpecifier BladeAppearSound = new SoundPathSpecifier("/Audio/Items/unsheath.ogg");
    public static readonly SoundSpecifier BladeBlockSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Heretic/parry.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProtectiveBladeComponent, ComponentShutdown>(OnBladeShutdown);
        SubscribeLocalEvent<ProtectiveBladeComponent, StoppedFollowingEntityEvent>(OnStopFollowing);

        SubscribeLocalEvent<ProtectiveBladesComponent, ProtectiveBladeUsedEvent>(OnBladeUsed);
        SubscribeLocalEvent<ProtectiveBladesComponent, BeforeDamageChangedEvent>(OnTakeDamage);
        SubscribeLocalEvent<ProtectiveBladesComponent, BeforeHarmfulActionEvent>(OnBeforeHarmfulAction,
            after: [typeof(SharedHereticAbilitySystem), typeof(RiposteeSystem)]);
        SubscribeLocalEvent<ProtectiveBladesComponent, ProjectileReflectAttemptEvent>(OnProjectileReflectAttempt);
        SubscribeLocalEvent<ProtectiveBladesComponent, HitScanReflectAttemptEvent>(OnHitscanReflectAttempt);

        CommandBinds.Builder
            .BindAfter(ContentKeyFunctions.ThrowItemInHand,
                new PointerInputCmdHandler(HandleThrowBlade),
                typeof(SharedHandsSystem))
            .Register<ProtectiveBladeSystem>();
    }


    private void OnStopFollowing(Entity<ProtectiveBladeComponent> ent, ref StoppedFollowingEntityEvent args)
    {
        var ev = new ProtectiveBladeUsedEvent(ent);
        RaiseLocalEvent(ent.Comp.User, ref ev);
    }


    private void OnBladeShutdown(Entity<ProtectiveBladeComponent> ent, ref ComponentShutdown args)
    {
        var ev = new ProtectiveBladeUsedEvent(ent);
        RaiseLocalEvent(ent.Comp.User, ref ev);
    }

    private void OnBladeUsed(Entity<ProtectiveBladesComponent> ent, ref ProtectiveBladeUsedEvent args)
    {
        ent.Comp.Blades.Remove(args.Used);
        RefreshBlades(ent);
    }

    private bool RefreshBlades(Entity<ProtectiveBladesComponent> ent)
    {
        ent.Comp.Blades = ent.Comp.Blades.Where(Exists).ToList();
        var count = ent.Comp.Blades.Count;
        if (ent.Comp.Blades.Count > 0)
        {
            if (ent.Comp.Blades.Count != count)
                Dirty(ent);
            return true;
        }

        RemCompDeferred(ent, ent.Comp);
        return false;
    }

    private bool HandleThrowBlade(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { Valid: true } player || !Exists(player) ||
            !coords.IsValid(EntityManager) || !_heretic.IsHereticOrGhoul(player) ||
            !TryComp(player, out ProtectiveBladesComponent? blades) ||
            HasComp<BlockProtectiveBladeShootComponent>(player))
            return false;

        if (!_hands.ActiveHandIsEmpty(player))
            return false;

        ThrowProtectiveBlade((player, blades), uid, _xform.ToWorldPosition(coords));
        return false;
    }

    private void OnProjectileReflectAttempt(Entity<ProtectiveBladesComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!RefreshBlades(ent))
            return;

        foreach (var blade in ent.Comp.Blades)
        {
            if (!TryComp<ReflectComponent>(blade, out var reflect))
                return;

            if (!_reflect.TryReflectProjectile((blade, reflect), ent, args.ProjUid))
                continue;

            args.Cancelled = true;
            PredictedQueueDel(blade);
            break;
        }
    }

    private void OnHitscanReflectAttempt(Entity<ProtectiveBladesComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected)
            return;

        if (!RefreshBlades(ent))
            return;

        foreach (var blade in ent.Comp.Blades)
        {
            if (!TryComp<ReflectComponent>(blade, out var reflect))
                return;

            if (!_reflect.TryReflectHitscan(
                    (blade, reflect),
                    ent,
                    args.Shooter,
                    args.SourceItem,
                    args.Direction,
                    args.Reflective,
                    args.Damage,
                    out var dir))
                continue;

            args.Direction = dir.Value;
            args.Reflected = true;
            PredictedQueueDel(blade);
            break;
        }
    }

    private void OnBeforeHarmfulAction(Entity<ProtectiveBladesComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Cancelled)
            return;

        if (!RefreshBlades(ent))
            return;

        PredictedQueueDel(ent.Comp.Blades[0]);

        _audio.PlayPvs(BladeBlockSound, ent);

        args.Cancel();
    }

    private void OnTakeDamage(Entity<ProtectiveBladesComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || args.Damage.GetTotal() < 5)
            return;

        if (!RefreshBlades(ent))
            return;

        PredictedQueueDel(ent.Comp.Blades[0]);

        _audio.PlayPvs(BladeBlockSound, ent);

        args.Cancelled = true;
    }

    public EntityUid AddProtectiveBlade(EntityUid ent, bool playSound = true)
    {
        // TODO: predict the code calling this in the future
        var pblade = Spawn(BladePrototype, Transform(ent).Coordinates);
        _follow.StartFollowingEntity(pblade, ent);
        if (playSound)
            _audio.PlayPvs(BladeAppearSound, ent);

        var blade = Comp<ProtectiveBladeComponent>(pblade);
        var blades = EnsureComp<ProtectiveBladesComponent>(ent);
        blade.User = ent;
        blades.Blades.Add(pblade);
        Dirty(pblade, blade);
        Dirty(ent, blades);

        /* Upstream removed this, but they randomise the start point so it's w/e
        // TODO: readd this in client startup, fucking idiot
        if (TryComp<OrbitVisualsComponent>(pblade, out var vorbit))
        {
            // test scenario: 4 blades are currently following our heretic.
            // making each one somewhat distinct from each other
            vorbit.Orbit = GetBlades(ent).Count / 5;
        }
        */

        return pblade;
    }

    public bool ThrowProtectiveBlade(Entity<ProtectiveBladesComponent> origin, EntityUid targetEntity, Vector2 target)
    {
        if (!RefreshBlades(origin))
            return false;

        var blade = origin.Comp.Blades[0];

        var pos = _xform.GetWorldPosition(origin);
        var direction = target - pos;

        var proj = PredictedSpawnAtPosition(BladeProjecilePrototype, Transform(origin).Coordinates);
        _gun.ShootProjectile(proj, direction, Vector2.Zero, origin, origin);
        if (targetEntity != EntityUid.Invalid)
            _gun.SetTarget(proj, targetEntity, out _);

        PredictedQueueDel(blade);

        // TODO: status effect entities
        _status.TryAddStatusEffect<BlockProtectiveBladeShootComponent>(origin,
            "BlockProtectiveBladeShoot",
            TimeSpan.FromSeconds(0.25f),
            true);

        return true;
    }
}
