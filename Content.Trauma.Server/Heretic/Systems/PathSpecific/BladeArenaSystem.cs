// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Common.Atmos;
using Content.Goobstation.Common.Body.Components;
using Content.Goobstation.Server.Wizard.Systems;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Wounds;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Hands.Systems;
using Content.Server.Roles;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Tag;
using Content.Trauma.Server.Heretic.Components.PathSpecific;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;
using Content.Trauma.Shared.Roles;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class BladeArenaSystem : SharedBladeArenaSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SanguineStrikeSystem _lifesteal = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;

    private readonly HashSet<EntityUid> _intersecting = new();

    [Dependency] private readonly EntityQuery<AirlockComponent> _airlockQuery = default!;
    [Dependency] private readonly EntityQuery<BladeArenaDetachedComponent> _detachedQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BladeArenaComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BladeArenaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BladeArenaComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<BladeArenaComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<BladeArenaComponent, HereticStateChangedEvent>(HereticStateChanged);

        SubscribeLocalEvent<HereticArenaParticipantComponent, ComponentStartup>(OnParticipantStartup);
        SubscribeLocalEvent<HereticArenaParticipantComponent, ComponentShutdown>(OnParticipantShutdown);
        SubscribeLocalEvent<HereticArenaParticipantComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<HereticArenaParticipantRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnGetBriefing(Entity<HereticArenaParticipantRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("roles-antag-arena-participant-description"));
    }

    private void OnEndCollide(Entity<BladeArenaComponent> ent, ref EndCollideEvent args)
    {
        var uid = args.OtherEntity;

        if (!TryComp(uid, out HereticArenaParticipantComponent? participant) || !participant.IsInsideArena)
            return;

        if (!participant.WasBreathingImmune)
            RemCompDeferred<SpecialBreathingImmunityComponent>(uid);

        if (!participant.WasPressureImmune)
            RemCompDeferred<SpecialPressureImmunityComponent>(uid);

        if (!participant.WasIgnoringGravity)
            RemCompDeferred<MovementIgnoreGravityComponent>(uid);

        participant.IsInsideArena = false;
        Dirty(uid, participant);
    }

    private void OnMobStateChanged(Entity<HereticArenaParticipantComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState != MobState.Alive || args.NewMobState <= args.OldMobState ||
            args.Origin is not { } origin || origin == ent.Owner ||
            !TryComp(origin, out HereticArenaParticipantComponent? victor) ||
            !ent.Comp.IsInsideArena || !victor.IsInsideArena)
            return;

        _heretic.TryGetHereticComponent(origin, out var heretic, out var mind);

        if (!victor.IsVictor && mind != default && TryComp(mind, out MindComponent? mindComp) &&
            _player.TryGetSessionById(mindComp.UserId, out var session))
        {
            var msg = Loc.GetString(heretic == null ? "blade-arena-crit-message" : "blade-arena-crit-message-heretic");
            _chatMan.ChatMessageToOne(ChatChannel.Server,
                msg,
                msg,
                default,
                false,
                session.Channel,
                Color.Purple);
        }

        if (!victor.IsVictor)
        {
            victor.IsVictor = true;
            Dirty(origin, victor);
        }

        _dmg.ChangeDamage(ent.Owner, ent.Comp.DamageOnCrit, true, targetPart: TargetBodyPart.Vital);

        if (heretic == null)
            return;

        if (TryComp(origin, out DamageableComponent? dmg))
            _lifesteal.LifeSteal((origin, dmg), victor.HereticHealPerCrit);

        // heal all wounds
        foreach (var (_, woundable) in _body.GetOrgans<WoundableComponent>(origin))
        {
            _container.EmptyContainer(woundable.Wounds);
        }
    }

    private void OnParticipantShutdown(Entity<HereticArenaParticipantComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Comp.Weapon))
            QueueDel(ent.Comp.Weapon);

        if (TerminatingOrDeleted(ent))
            return;

        if (Exists(ent.Comp.Mind) && TryComp(ent.Comp.Mind.Value, out MindComponent? mind) &&
            _role.MindHasRole<HereticArenaParticipantRoleComponent>(ent.Comp.Mind.Value))
            _role.MindRemoveRole<HereticArenaParticipantRoleComponent>((ent.Comp.Mind.Value, mind));
    }

    private void OnParticipantStartup(Entity<HereticArenaParticipantComponent> ent, ref ComponentStartup args)
    {
        if (_mind.TryGetMind(ent, out var mindId, out var mind))
        {
            ent.Comp.Mind = mindId;
            if (HasComp<HereticComponent>(mindId))
                return;
            if (!_role.MindHasRole<HereticArenaParticipantComponent>(mindId))
            {
                _antag.SendBriefing(ent, Loc.GetString("roles-antag-arena-participant-description"), Color.Red, null);
                _role.MindAddRole(mindId, ent.Comp.RoleProto, mind);
            }
        }

        if (!TryComp(ent, out HandsComponent? hands))
            return;

        var blade = Spawn(ent.Comp.WeaponProto, Transform(ent).Coordinates);
        ent.Comp.Weapon = blade;
        _hands.PickupOrDrop(ent, blade, handsComp: hands);
    }

    private void HereticStateChanged(Entity<BladeArenaComponent> ent, ref HereticStateChangedEvent args)
    {
        if (!args.IsDead || args.Temporary)
            return;

        QueueDel(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // If floor is spawned on space tile and outer wall spawns on it on the same tick -
        // it doesn't get anchored, so we have to anchor it on next tick
        var query = EntityQueryEnumerator<BladeArenaOuterWallComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.Anchored)
                continue;

            _transform.AttachToGridOrMap(uid, xform);
            _transform.AnchorEntity(uid, xform);
        }
    }

    private void OnStartCollide(Entity<BladeArenaComponent> ent, ref StartCollideEvent args)
    {
        var uid = args.OtherEntity;

        if (!HasComp<MobStateActionsComponent>(uid) ||
            !HasComp<HumanoidProfileComponent>(uid) ||
            HasComp<GhoulComponent>(uid))
            return;

        ent.Comp.Participants.Add(uid);

        var participant = EnsureComp<HereticArenaParticipantComponent>(uid);

        if (participant.IsInsideArena)
            return;

        participant.WasBreathingImmune = EnsureComp<SpecialBreathingImmunityComponent>(uid, out _);
        participant.WasPressureImmune = EnsureComp<SpecialPressureImmunityComponent>(uid, out _);
        participant.WasIgnoringGravity = EnsureComp<MovementIgnoreGravityComponent>(uid, out _);

        participant.IsInsideArena = true;
        Dirty(uid, participant);
    }

    private void OnShutdown(Entity<BladeArenaComponent> ent, ref ComponentShutdown args)
    {
        foreach (var participant in ent.Comp.Participants)
        {
            if (TerminatingOrDeleted(participant))
                continue;

            RemCompDeferred<HereticArenaParticipantComponent>(participant);
        }

        if (TerminatingOrDeleted(ent.Comp.Grid) || !TryComp(ent.Comp.Grid, out MapGridComponent? grid))
            return;

        foreach (var uid in ent.Comp.SpawnedEntities)
        {
            if (!TerminatingOrDeleted(uid))
                Del(uid);
        }

        foreach (var uid in ent.Comp.DetachedEntities)
        {
            if (!_detachedQuery.TryComp(uid, out var comp))
                continue;

            var xform = Transform(uid);
            var meta = MetaData(uid);
            _transform.SetCoordinates((uid, xform, meta), comp.OriginalCoords, rotation: comp.OriginalRotation);
            _transform.AnchorEntity(uid, xform);
        }

        var tileId = _proto.Index(ent.Comp.Tile).TileId;

        foreach (var indices in ent.Comp.TilesToRestore)
        {
            if (!_map.TryGetTileRef(ent.Comp.Grid.Value, grid, indices, out var tileRef) ||
                tileRef.Tile.TypeId != tileId)
                continue;

            _tile.DeconstructTile(tileRef, false);
        }
    }

    private void OnMapInit(Entity<BladeArenaComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Radius < 1)
        {
            QueueDel(ent);
            return;
        }

        var coords = _transform.GetMapCoordinates(ent);
        if (!_mapManager.TryFindGridAt(coords, out var grid, out var gridComp))
        {
            QueueDel(ent);
            return;
        }

        var center = _map.TileIndicesFor(grid, gridComp, coords);
        if (!_map.TryGetTileRef(grid, gridComp, center, out var centerTile))
        {
            QueueDel(ent);
            return;
        }

        var originIndices = centerTile.GridIndices;
        Entity<MapGridComponent> gridEnt = (grid, gridComp);
        var bounds = _lookup.GetWorldBounds(centerTile);
        bounds.Box = bounds.Box.Enlarged(ent.Comp.Radius).Scale(1f - 0.2f / ent.Comp.Radius);
        _intersecting.Clear();
        _lookup.GetEntitiesIntersecting(grid, bounds, _intersecting, LookupFlags.Static);
        foreach (var uid in _intersecting)
        {
            if (_tag.HasTag(uid, ent.Comp.WallTag))
            {
                DetachEntity(uid, originIndices, gridEnt, ent.Comp, ent.Comp.WallReplacement);
                continue;
            }

            if (_tag.HasTag(uid, ent.Comp.WindowTag))
            {
                DetachEntity(uid, originIndices, gridEnt, ent.Comp, ent.Comp.WindowReplacement);
                continue;
            }

            if (!_airlockQuery.HasComp(uid))
                continue;

            DetachEntity(uid, originIndices, gridEnt, ent.Comp, null);
        }

        for (var i = -ent.Comp.Radius; i < ent.Comp.Radius; i++)
        {
            var a = originIndices + new Vector2i(i, ent.Comp.Radius);
            var c = originIndices + new Vector2i(ent.Comp.Radius, -i);
            var b = originIndices + new Vector2i(-i, -ent.Comp.Radius);
            var d = originIndices + new Vector2i(-ent.Comp.Radius, i);
            SpawnOuterWall(a, ent.Comp, gridEnt);
            SpawnOuterWall(b, ent.Comp, gridEnt);
            SpawnOuterWall(c, ent.Comp, gridEnt);
            SpawnOuterWall(d, ent.Comp, gridEnt);
        }

        var shape = new PolygonShape();
        shape.SetAsBox(Box2.CenteredAround(Vector2.Zero, bounds.Box.Size));
        _fixtures.TryCreateFixture(ent,
            shape,
            "fix1",
            collisionLayer: ent.Comp.Layer,
            collisionMask: ent.Comp.Layer,
            hard: false);
        _physics.SetCanCollide(ent, true, force: true);
    }

    private void DetachEntity(EntityUid uid,
        Vector2i originIndices,
        Entity<MapGridComponent> grid,
        BladeArenaComponent arena,
        EntProtoId? replaceWith)
    {
        var xform = Transform(uid);
        if (xform.ParentUid != grid.Owner)
            return;

        var coords = xform.Coordinates;
        var indices = _map.TileIndicesFor(grid, coords);
        var relative = indices - originIndices;
        var detached = EnsureComp<BladeArenaDetachedComponent>(uid);
        detached.OriginalCoords = coords;
        detached.OriginalRotation = xform.LocalRotation;
        arena.DetachedEntities.Add(uid);
        _transform.DetachEntity(uid, xform);

        if (replaceWith is not { } replacement || _tag.HasAnyTag(uid, arena.NoReplaceTags) ||
            Math.Abs(relative.X) >= arena.Radius || Math.Abs(relative.Y) >= arena.Radius)
            return;

        SpawnEntity(replacement, coords, arena, grid);
    }

    private void SpawnOuterWall(Vector2i indices,
        BladeArenaComponent arena,
        Entity<MapGridComponent> grid)
    {
        var spawned = Spawn(arena.OuterWall, _map.GridTileToLocal(grid, grid, indices));
        arena.SpawnedEntities.Add(spawned);
        if (_map.CollidesWithGrid(grid, grid, indices))
            _transform.AnchorEntity((spawned, Transform(spawned)), grid);
    }

    private void SpawnEntity(EntProtoId proto,
        EntityCoordinates coords,
        BladeArenaComponent arena,
        Entity<MapGridComponent> grid)
    {
        var spawned = Spawn(proto, coords);
        _transform.AnchorEntity((spawned, Transform(spawned)), grid);
        arena.SpawnedEntities.Add(spawned);
    }
}
