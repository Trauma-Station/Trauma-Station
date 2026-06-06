// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Audio;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Effects;
using Content.Shared.Gravity;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Content.Shared.Weather;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

// void path heretic exclusive
public sealed partial class AristocratSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _rand = default!;
    [Dependency] private IPrototypeManager _prot = default!;
    [Dependency] private IMapManager _mapMan = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private VoidCurseSystem _voidcurse = default!;
    [Dependency] private ServerGlobalSoundSystem _globalSound = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedPoweredLightSystem _light = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private HereticSystem _heretic = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private EntityQuery<AirlockComponent> _airlockQuery = default!;
    [Dependency] private EntityQuery<StatusEffectsComponent> _statusQuery = default!;

    private static readonly EntProtoId IceTilePrototype = "IceCrust";
    private static readonly EntProtoId IceWallPrototype = "WallIce";
    private static readonly EntProtoId SnowfallMagic = "WeatherSnowfallMagic";
    private static readonly ProtoId<ContentTileDefinition> SnowTilePrototype = "FloorAstroSnow";
    private static readonly ProtoId<TagPrototype> Window = "Window";

    private static readonly TimeSpan ConduitDelay = TimeSpan.FromSeconds(2);
    private TimeSpan _nextUpdate = TimeSpan.Zero;

    private readonly HashSet<Entity<FreezableWallComponent>> _walls = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AristocratComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AristocratComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AristocratComponent, MobStateChangedEvent>(OnMobStateChange);
    }

    private void OnStartup(Entity<AristocratComponent> ent, ref ComponentStartup args)
    {
        if (!HasComp<MobStateComponent>(ent))
            return; // fuck off tests

        BeginWaltz(ent);
        DoVoidAnnounce(ent, "begin");
        _movement.RefreshWeightlessModifiers(ent);
        _gravity.RefreshWeightless(ent.Owner, true);
    }

    private bool CheckOtherAristocrats(Entity<AristocratComponent> ent)
    {
        var others = EntityQueryEnumerator<AristocratComponent, MobStateComponent>();
        while (others.MoveNext(out var other, out _, out var stateComp))
        {
            if (ent.Owner == other || stateComp.CurrentState == MobState.Dead)
                continue;

            return true;
        }

        return false;
    }

    private void DoVoidAnnounce(Entity<AristocratComponent> ent, string context)
    {
        if (CheckOtherAristocrats(ent))
            return;

        var xform = Transform(ent);

        var victims = EntityQueryEnumerator<ActorComponent, MobStateComponent>();
        while (victims.MoveNext(out var victim, out var actorComp, out var stateComp))
        {
            var xformVictim = Transform(victim);

            if (xformVictim.MapUid != xform.MapUid || stateComp.CurrentState == MobState.Dead ||
                ent.Owner ==
                victim) // DoVoidAnnounce doesn't happen when there's other (alive) ascended void heretics, so you only have to exclude the user
                continue;

            _popup.PopupEntity(Loc.GetString($"void-ascend-{context}"),
                victim,
                actorComp.PlayerSession,
                PopupType.LargeCaution);
        }
    }

    private void BeginWaltz(Entity<AristocratComponent> ent)
    {
        if (CheckOtherAristocrats(ent))
            return;

        _globalSound.DispatchStationEventMusic(ent,
            ent.Comp.VoidsEmbrace,
            StationEventMusicType.VoidAscended,
            AudioParams.Default.WithLoop(true));

        // the fog (snow) is coming
        var xform = Transform(ent);
        _weather.TrySetWeather(xform.MapID, SnowfallMagic, out _);
    }

    private void EndWaltz(Entity<AristocratComponent> ent)
    {
        if (CheckOtherAristocrats(ent))
            return;

        _globalSound.StopStationEventMusic(ent, StationEventMusicType.VoidAscended);

        if (Transform(ent).MapUid is { } map)
            _weather.TryRemoveWeather(map, SnowfallMagic);
    }

    private void OnMobStateChange(Entity<AristocratComponent> ent, ref MobStateChangedEvent args)
    {
        var stateComp = args.Component;

        if (stateComp.CurrentState == MobState.Dead)
        {
            ent.Comp.HasDied = true;
            EndWaltz(ent); // its over bros
            DoVoidAnnounce(ent, "end");
        }

        // in the rare case that they are revived for whatever reason
        if (stateComp.CurrentState == MobState.Alive && ent.Comp.HasDied)
        {
            ent.Comp.HasDied = false;
            BeginWaltz(ent); // we're back bros
            DoVoidAnnounce(ent, "restart");
        }
    }


    private void OnShutdown(Entity<AristocratComponent> ent, ref ComponentShutdown args)
    {
        EndWaltz(ent); // its over bros
        DoVoidAnnounce(ent, "end");

        if (TerminatingOrDeleted(ent))
            return;

        _movement.RefreshWeightlessModifiers(ent);
        _gravity.RefreshWeightless(ent.Owner, false);
    }

    private List<EntityCoordinates> GetTiles(EntityCoordinates coords, int range)
    {
        var tiles = new List<EntityCoordinates>();

        for (var y = -range; y <= range; y++)
        {
            for (var x = -range; x <= range; x++)
            {
                var offset = new Vector2(x, y);

                var pos = coords.Offset(offset).SnapToGrid(EntityManager, _mapMan);
                tiles.Add(pos);
            }
        }

        return tiles;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<AristocratComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var aristocrat, out var xform))
        {
            if (aristocrat.NextUpdate > now)
                continue;

            aristocrat.NextUpdate = now + aristocrat.UpdateDelay;

            Cycle((uid, aristocrat, xform));
        }

        if (_nextUpdate > now)
            return;

        _nextUpdate = now + ConduitDelay;

        HashSet<EntityUid> ignored = new();
        var conduitQuery = EntityQueryEnumerator<VoidConduitComponent, TransformComponent>();
        while (conduitQuery.MoveNext(out var uid, out var conduit, out var xform))
        {
            if (!conduit.Active) // Skip first iteration
            {
                conduit.Active = true;
                continue;
            }

            FreezeAtmos((uid, xform));

            var (pos, rot) = _xform.GetWorldPositionRotation(xform);

            var box = Box2.CenteredAround(pos, Vector2.One * (1f + conduit.Range * 2f));
            var rotated = new Box2Rotated(box, rot, pos);

            List<EntityUid> affected = new();
            var result = _lookup.GetEntitiesIntersecting(xform.MapID, rotated);
            foreach (var ent in result)
            {
                if (ignored.Contains(ent))
                    continue;

                if (_heretic.IsHereticOrGhoul(ent))
                {
                    ignored.Add(ent);
                    if (_statusQuery.TryComp(ent, out var status))
                    {
                        _status.TryAddStatusEffect<PressureImmunityComponent>(ent,
                            "PressureImmunity",
                            TimeSpan.FromSeconds(2),
                            true,
                            status);
                    }

                    continue;
                }

                if (_voidcurse.DoCurse(ent))
                {
                    ignored.Add(ent);
                    affected.Add(ent);
                    continue;
                }

                var dmg = conduit.StructureDamage;

                if (_airlockQuery.HasComp(ent))
                {
                    _audio.PlayPvs(conduit.AirlockDamageSound, Transform(ent).Coordinates);
                    ignored.Add(ent);
                    affected.Add(ent);
                    _damage.TryChangeDamage(ent,
                        dmg * _rand.NextFloat(conduit.MinMaxAirlockDamageMultiplier.X,
                            conduit.MinMaxAirlockDamageMultiplier.Y),
                        origin: ent);
                }
                else if (_tag.HasTag(ent, Window))
                {
                    _audio.PlayPvs(conduit.WindowDamageSound, Transform(ent).Coordinates);
                    ignored.Add(ent);
                    affected.Add(ent);
                    _damage.TryChangeDamage(ent,
                        dmg * _rand.NextFloat(conduit.MinMaxWindowDamageMultiplier.X,
                            conduit.MinMaxWindowDamageMultiplier.Y),
                        origin: ent);
                }
            }

            if (affected.Count > 0)
                _color.RaiseEffect(Color.Black, affected, Filter.Pvs(uid, 3f, EntityManager));

            if (conduit.Range < conduit.MaxRange)
            {
                conduit.Range++;
                Dirty(uid, conduit);
            }
        }
    }

    private void Cycle(Entity<AristocratComponent, TransformComponent> ent)
    {
        if (ent.Comp1.HasDied) // powers will only take effect for as long as we're alive
            return;

        var step = ent.Comp1.UpdateStep;

        if (step % 100 == 0)
        {
            step = 10;
        }

        if (step % 10 == 0)
            FreezeNoobs(ent);

        switch (step % 4)
        {
            case 0:
                ExtinguishFires(ent);
                break;
            case 1:
                FreezeAtmos((ent.Owner, ent.Comp2));
                break;
            case 2:
                DoChristmas(ent);
                break;
            case 3:
                SpookyLights(ent);
                break;
        }

        ent.Comp1.UpdateStep++;
    }

    // makes shit cold
    private void FreezeAtmos(Entity<TransformComponent> ent)
    {
        var mix = _atmos.GetTileMixture((ent, Transform(ent)));
        var freezingTemp = Atmospherics.T0C;

        if (mix != null)
        {
            if (mix.Temperature > freezingTemp)
                mix.Temperature = freezingTemp;

            mix.Temperature -= 100f;
        }
    }

    // extinguish gases on tiles
    private void ExtinguishFiresTiles(Entity<AristocratComponent, TransformComponent> ent)
    {
        var coords = ent.Comp2.Coordinates;
        var tiles = GetTiles(coords, (int) ent.Comp1.Range);

        if (tiles.Count == 0)
            return;

        foreach (var pos in tiles)
        {
            var tile = _turf.GetTileRef(pos);

            if (tile == null)
                continue;

            _atmos.HotspotExtinguish(tile.Value.GridUid, tile.Value.GridIndices);
        }
    }

    // extinguish ppl and stuff
    private void ExtinguishFires(Entity<AristocratComponent, TransformComponent> ent)
    {
        var coords = ent.Comp2.Coordinates;
        var fires = _lookup.GetEntitiesInRange<FlammableComponent>(coords, ent.Comp1.Range);

        foreach (var (uid, flam) in fires)
        {
            if (flam.OnFire)
                _flammable.Extinguish(uid, flam);
        }

        ExtinguishFiresTiles(ent);
    }

    // replaces certain things with their winter analogue (amongst other things)
    private void DoChristmas(Entity<AristocratComponent, TransformComponent> ent)
    {
        SpawnTiles(ent);

        var coords = ent.Comp2.Coordinates;

        _walls.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp1.Range, _walls, LookupFlags.Static);

        foreach (var uid in _walls)
        {
            if (!_rand.Prob(.45f))
                continue;

            Spawn(IceWallPrototype, Transform(uid).Coordinates);
            QueueDel(uid);
        }
    }

    // kill the lights
    private void SpookyLights(Entity<AristocratComponent, TransformComponent> ent)
    {
        var coords = ent.Comp2.Coordinates;
        var lights = _lookup.GetEntitiesInRange<PoweredLightComponent>(coords, ent.Comp1.Range, LookupFlags.Static);

        foreach (var (uid, light) in lights)
        {
            _light.TryDestroyBulb(uid, light);
        }
    }

    // curses noobs
    private void FreezeNoobs(Entity<AristocratComponent, TransformComponent> ent)
    {
        var coords = ent.Comp2.Coordinates;
        var noobs = _lookup.GetEntitiesInRange<MobStateComponent>(coords, ent.Comp1.Range);

        foreach (var noob in noobs)
        {
            // Apply up to 3 void chill stacks
            _voidcurse.DoCurse(noob, 1, 3);
        }
    }

    private void SpawnTiles(Entity<AristocratComponent, TransformComponent> ent)
    {
        if (!Exists(ent.Comp2.GridUid))
            return;

        var tiles = GetTiles(ent.Comp2.Coordinates, (int) ent.Comp1.Range);

        if (tiles.Count == 0)
            return;

        // it's christmas!!
        foreach (var pos in tiles)
        {
            if (!_rand.Prob(.3f))
                continue;

            var tile = _turf.GetTileRef(pos);

            if (tile == null)
                continue;

            var newTile = _prot.Index(SnowTilePrototype);
            _tile.ReplaceTile(tile.Value, newTile);

            // TODO: turf or something bruh
            var condition = _lookup.GetEntitiesInRange(pos, .1f, LookupFlags.Static | LookupFlags.Sensors)
                .All(e => Prototype(e)?.ID != IceTilePrototype.Id);
            if (condition)
                Spawn(IceTilePrototype, pos);
        }
    }
}
