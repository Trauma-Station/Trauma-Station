using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Medical.Common.Targeting;
using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared._Goobstation.Wizard;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Explosion;
using Content.Shared.Heretic;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Slippery;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Random;

namespace Content.Shared._Shitcode.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    public static readonly ProtoId<ContentTileDefinition> RustTile = "PlatingRust";
    public static readonly ProtoId<TagPrototype> Wall = "Wall";
    public static readonly EntProtoId StatusEffectStunned = "StatusEffectStunned";

    private readonly HashSet<Entity<FixturesComponent>> _lookupFixtures = new();
    private readonly HashSet<Entity<MobStateComponent>> _lookupMobs = new();

    public static readonly Dictionary<EntProtoId, EntProtoId> Transformations = new()
    {
        { "WallSolid", "WallSolidRust" },
        { "WallReinforced", "WallReinforcedRust" },
    };

    protected virtual void SubscribeRust()
    {
        SubscribeLocalEvent<EventHereticRustConstruction>(OnRustConstruction);
        SubscribeLocalEvent<EventHereticAggressiveSpread>(OnAggressiveSpread);
        SubscribeLocalEvent<EventHereticEntropicPlume>(OnEntropicPlume);

        SubscribeLocalEvent<RustbringerComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<RustbringerComponent, KnockDownAttemptEvent>(OnKnockDownAttempt);
        SubscribeLocalEvent<RustbringerComponent, BeforeStatusEffectAddedEvent>(OnBeforeStatusEffect);
        SubscribeLocalEvent<RustbringerComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<RustbringerComponent, GetExplosionResistanceEvent>(OnGetExplosionResists);
        SubscribeLocalEvent<RustbringerComponent, ElectrocutionAttemptEvent>(OnElectrocuteAttempt);
        SubscribeLocalEvent<RustbringerComponent, BeforeHarmfulActionEvent>(OnBeforeHarmfulAction);
        SubscribeLocalEvent<RustbringerComponent, DamageModifyEvent>(OnModifyDamage);
    }

    private void OnModifyDamage(Entity<RustbringerComponent> ent, ref DamageModifyEvent args)
    {
        if (!IsOnRust(ent))
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.ModifierSet);
    }

    private void OnBeforeHarmfulAction(Entity<RustbringerComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Cancelled || args.Type == HarmfulActionType.Harm)
            return;

        if (!IsOnRust(ent))
            return;

        args.Cancel();
    }

    private void OnElectrocuteAttempt(Entity<RustbringerComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        if (!IsTileRust(Transform(ent).Coordinates, out _))
            return;

        args.Cancel();
    }

    private void OnGetExplosionResists(Entity<RustbringerComponent> ent, ref GetExplosionResistanceEvent args)
    {
        if (!IsOnRust(ent))
            return;

        args.DamageCoefficient *= 0f;
    }

    private void OnSlipAttempt(Entity<RustbringerComponent> ent, ref SlipAttemptEvent args)
    {
        args.NoSlip |= IsOnRust(ent);
    }

    private void OnKnockDownAttempt(EntityUid uid, RustbringerComponent comp, ref KnockDownAttemptEvent args)
    {
        args.Cancelled |= IsOnRust(uid);
    }

    private void OnBeforeStatusEffect(Entity<RustbringerComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == StatusEffectStunned)
            args.Cancelled |= IsOnRust(ent);
    }

    private void OnBeforeStaminaDamage(Entity<RustbringerComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled |= IsOnRust(ent);
    }

    public bool IsTileRust(EntityCoordinates coords, [NotNullWhen(true)] out Vector2i? tileCoords)
    {
        tileCoords = null;
        if (!_mapMan.TryFindGridAt(_transform.ToMapCoordinates(coords), out var gridUid, out var mapGrid))
            return false;

        var tileRef = _map.GetTileRef(gridUid, mapGrid, coords);
        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId];

        tileCoords = tileRef.GridIndices;
        return tileDef.ID == RustTile;
    }

    public bool IsOnRust(EntityUid uid)
        => IsTileRust(Transform(uid).Coordinates, out _);

    private void OnEntropicPlume(EventHereticEntropicPlume args)
    {
        var uid = args.Performer;

        if (!TryUseAbility(args))
            return;

        var xform = Transform(uid);

        var (pos, rot) = _transform.GetWorldPositionRotation(xform);

        var dir = rot.ToWorldVec();

        var mapPos = new MapCoordinates(pos + dir * args.Offset, xform.MapID);

        var plume = PredictedSpawnAtPosition(args.Proto, _transform.ToCoordinates(mapPos));

        RustObjectsInRadius(mapPos, args.Radius, args.TileRune, args.LookupRange, args.RustStrength);

        _gun.ShootProjectile(plume, dir, Vector2.Zero, uid, uid, args.Speed);
        _gun.SetTarget(plume, null, out _);
    }

    public void RustObjectsInRadius(MapCoordinates mapPos,
        float radius,
        string tileRune,
        float lookupRange,
        int rustStrength)
    {
        var circle = new Circle(mapPos.Position, radius);
        var grids = new List<Entity<MapGridComponent>>();
        var box = Box2.CenteredAround(mapPos.Position, new Vector2(radius, radius));
        _mapMan.FindGridsIntersecting(mapPos.MapId, box, ref grids);

        var tiles = new List<(EntityCoordinates, TileRef, EntityUid, MapGridComponent)>();
        foreach (var grid in grids)
        {
            tiles.AddRange(_map.GetTilesIntersecting(grid.Owner, grid.Comp, circle)
                .Select(x => (_map.GridTileToLocal(grid.Owner, grid.Comp, x.GridIndices), x, grid.Owner, grid.Comp)));
        }

        foreach (var (coords, tileRef, gridUid, mapGrid) in tiles)
        {
            if (CanRustTile((ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId]))
                MakeRustTile(gridUid, mapGrid, tileRef, tileRune);

            foreach (var toRust in Lookup.GetEntitiesInRange(coords, lookupRange, LookupFlags.Static))
            {
                TryMakeRustWall(toRust, rustStrengthOverride: rustStrength);
            }
        }
    }

    private void OnAggressiveSpread(EventHereticAggressiveSpread args)
    {
        if (!TryUseAbility(args))
            return;

        if (_net.IsClient)
            return;

        var uid = args.Performer;

        Heretic.TryGetHereticComponent(uid, out var heretic, out _);
        var effectiveStage = MathF.Max(heretic?.PathStage ?? 9f - 4f, 1f);
        var multiplier = heretic?.CurrentPath is null or "Rust" ? MathF.Sqrt(effectiveStage) : 1f;

        var aoeRadius = MathF.Max(args.AoeRadius, args.AoeRadius * multiplier);
        var range = MathF.Max(args.Range, args.Range * multiplier);

        var mapPos = _transform.GetMapCoordinates(args.Performer);
        var box = Box2.CenteredAround(mapPos.Position, new Vector2(range, range));
        var circle = new Circle(mapPos.Position, range);
        var grids = new List<Entity<MapGridComponent>>();
        _mapMan.FindGridsIntersecting(mapPos.MapId, box, ref grids);

        var tiles = new List<(EntityCoordinates, TileRef, EntityUid, MapGridComponent)>();
        foreach (var grid in grids)
        {
            tiles.AddRange(_map.GetTilesIntersecting(grid.Owner, grid.Comp, circle)
                .Select(x => (_map.GridTileToLocal(grid.Owner, grid.Comp, x.GridIndices), x, grid.Owner, grid.Comp)));
        }

        foreach (var (coords, tileRef, gridUid, mapGrid) in tiles)
        {
            var distanceToCaster = (_transform.ToMapCoordinates(coords).Position - mapPos.Position).Length();
            var chanceOfNotRusting = Math.Clamp((MathF.Max(distanceToCaster, 1f) - 1f) / (aoeRadius - 1f), 0f, 1f);

            if (_random.Prob(chanceOfNotRusting))
                continue;

            if (CanRustTile((ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId]))
                MakeRustTile(gridUid, mapGrid, tileRef, args.TileRune);

            foreach (var toRust in Lookup.GetEntitiesInRange(coords, args.LookupRange, LookupFlags.Static))
            {
                TryMakeRustWall(toRust, rustStrengthOverride: args.RustStrength);
            }
        }
    }

    public bool CanSurfaceBeRusted(EntityUid target, EntityUid? user, HereticComponent? heretic, out int surfaceStrength)
    {
        surfaceStrength = 0;

        if (!TryComp(target, out RustRequiresPathStageComponent? requiresPathStage))
            return true;

        var stage = heretic == null ? 10 : heretic.PathStage;
        surfaceStrength = requiresPathStage.PathStage;

        if (surfaceStrength <= stage)
            return true;

        if (user != null)
            Popup.PopupClient(Loc.GetString("heretic-ability-fail-rust-stage-low"), user.Value, user.Value);

        return false;
    }

    public bool CanRustTile(ContentTileDefinition tile)
    {
        return tile.ID != RustTile && !tile.Indestructible &&
               !(tile.DeconstructTools.Count == 0 && tile.Weather);
    }

    public void MakeRustTile(EntityUid gridUid, MapGridComponent mapGrid, TileRef tileRef, EntProtoId tileRune)
    {
        var plating = _tileDefinitionManager[RustTile];
        _map.SetTile(gridUid, mapGrid, tileRef.GridIndices, new Tile(plating.TileId));

        // Serverside spawn because it gets randomized sprite offset clientside and predict would break it
        if (_net.IsServer)
            Spawn(tileRune, new EntityCoordinates(gridUid, tileRef.GridIndices));
    }

    public bool TryMakeRustWall(EntityUid target, EntityUid? user = null, HereticComponent? heretic = null, int? rustStrengthOverride = null)
    {
        var canRust = CanSurfaceBeRusted(target, user, heretic, out var surfaceStrength);

        if (TryComp(target, out RustedWallComponent? wall))
        {
            if (wall.LifeStage != ComponentLifeStage.Running)
                return true;

            if (surfaceStrength > (rustStrengthOverride ?? heretic?.PathStage ?? -1))
                return false;

            PredictedDel(target);
            return true;
        }

        var proto = Prototype(target);

        var targetEntity = target;

        // Check transformations (walls into rusted walls)
        if (proto != null && Transformations.TryGetValue(proto.ID, out var transformation))
        {
            if (!canRust)
                return false;

            var xform = Transform(target);
            var rotation = xform.LocalRotation;
            var coords = xform.Coordinates;

            PredictedDel(target);

            targetEntity =
                PredictedSpawnAttachedTo(transformation, coords, rotation: rotation);
        }

        if (TerminatingOrDeleted(targetEntity) || !_tag.HasTag(targetEntity, Wall))
            return false;

        if (targetEntity == target && !canRust)
            return false;

        EnsureComp<RustedWallComponent>(targetEntity);

        // If targetEntity is target (which means no transformations were performed) - we add rust overlay
        if (targetEntity == target)
            EnsureComp<RustOverlayComponent>(targetEntity);

        // Rune gets sprite and offset randomized clientside, predict would break it
        if (_net.IsServer)
            EnsureComp<RustRuneComponent>(targetEntity);

        return true;
    }

    private void OnRustConstruction(EventHereticRustConstruction args)
    {
        if (!TryUseAbility( args, false))
            return;

        var ent = args.Performer;

        if (!IsTileRust(args.Target, out var pos))
        {
            Popup.PopupClient(Loc.GetString("heretic-ability-fail-tile-not-rusted"), ent, ent);
            return;
        }

        var mask = CollisionGroup.LowImpassable | CollisionGroup.MidImpassable | CollisionGroup.HighImpassable |
                   CollisionGroup.Impassable;

        _lookupFixtures.Clear();
        Lookup.GetEntitiesInRange(args.Target, args.ObstacleCheckRange, _lookupFixtures, LookupFlags.Static);
        foreach (var (_, fix) in _lookupFixtures)
        {
            if (fix.Fixtures.All(x => (x.Value.CollisionLayer & (int) mask) == 0))
                continue;

            Popup.PopupClient(Loc.GetString("heretic-ability-fail-tile-occupied"), ent, ent);
            return;
        }

        var mapCoords = _transform.ToMapCoordinates(args.Target);

        Lookup.GetEntitiesInRange(args.Target, args.MobCheckRange, _lookupMobs, LookupFlags.Dynamic);
        foreach (var (entity, _) in _lookupMobs)
        {
            var dir = _transform.GetWorldPosition(entity) - mapCoords.Position;
            if (dir.LengthSquared() < 0.001f)
                continue;
            _throw.TryThrow(entity, dir.Normalized() * args.ThrowRange, args.ThrowSpeed);
            _stun.KnockdownOrStun(entity, args.KnockdownTime);
            if (entity != args.Performer)
                _dmg.TryChangeDamage(entity, args.Damage, targetPart: TargetBodyPart.All);
        }

        args.Handled = true;

        var coords = new EntityCoordinates(args.Target.EntityId, pos.Value);
        var wall = PredictedSpawnAttachedTo(args.RustedWall, coords);
        EnsureComp<RustRuneComponent>(wall);

        if (_net.IsServer)
            RaiseNetworkEvent(new StopTargetingEvent(), args.Performer);

        _audio.PlayPredicted(args.Sound, args.Target, args.Performer);
    }
}
