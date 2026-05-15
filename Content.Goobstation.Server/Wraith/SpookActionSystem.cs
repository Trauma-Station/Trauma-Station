// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Components;
using Content.Goobstation.Shared.Wraith.Events;
using Content.Goobstation.Shared.Wraith.Spook;
using Content.Goobstation.Shared.Wraith.WraithPoints;
using Content.Server.Actions;
using Content.Server.Doors.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Actions.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Humanoid;

namespace Content.Goobstation.Server.Wraith;

// TODO: most of this shit just looks up X component in a range then does a thing to N of them, this should use entity effects instead of reinventing the wheel 50 times
public sealed partial class SpookActionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private DoorSystem _door = default!;
    [Dependency] private SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private SmokeSystem _smoke = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<DoorComponent> _doorQuery = default!;
    [Dependency] private EntityQuery<EntityStorageComponent> _entityStorageQuery = default!;
    [Dependency] private EntityQuery<ActionComponent> _actionQuery = default!;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;

    private HashSet<Entity<ApcComponent>> _apcs = new();
    private HashSet<Entity<FlammableComponent>> _fireTargets = new();
    private HashSet<Entity<PoweredLightComponent>> _lights = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpookMarkComponent, SpookEvent>(OnSpookEvent);

        SubscribeLocalEvent<FlipLightsComponent, FlipLightsEvent>(OnFlipLights);
        SubscribeLocalEvent<BurnLightsComponent, BurnLightsEvent>(OnBurnLights);
        SubscribeLocalEvent<OpenDoorsSpookComponent, OpenDoorsSpookEvent>(OnOpenDoors);
        SubscribeLocalEvent<CreateSpookSmokeComponent, CreateSmokeSpookEvent>(OnCreateSmoke);
        SubscribeLocalEvent<CreateEctoplasmComponent, CreateEctoplasmEvent>(OnCreateEctoplasm);
        SubscribeLocalEvent<SapAPCComponent, SapApcEvent>(OnSapAPC);
        SubscribeLocalEvent<RandomSpookComponent, RandomSpookEvent>(OnRandomSpook);
    }
    // todo: Haunt PDAs (ngl js do it in part 2 im tired)

    private void OnSpookEvent(Entity<SpookMarkComponent> ent, ref SpookEvent args)
    {
        if (ent.Comp.SpookEntity is {} spook)
            QueueDel(spook);

        var spookEnt = SpawnAtPosition(ent.Comp.Spook, args.Target);
        ent.Comp.SpookEntity = spookEnt;
        _popup.PopupEntity(Loc.GetString("spook-on-create"), ent.Owner, PopupType.Medium);

        args.Handled = true;
    }

    private void OnFlipLights(Entity<FlipLightsComponent> ent, ref FlipLightsEvent args)
    {
        // taken from ghost boo system

        if (args.Handled)
            return;

        var coords = Transform(args.Performer).Coordinates;
        _lights.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.FlipLightRadius, _lights);
        var lights = _lights.ToList();
        _random.Shuffle(lights);

        var booCounter = 0;
        foreach (var entity in lights)
        {
            var ev = new GhostBooEvent();
            RaiseLocalEvent(entity, ev);

            if (ev.Handled)
                booCounter++;

            if (booCounter >= ent.Comp.FlipLightMaxTargets)
                break;
        }

        _popup.PopupEntity(Loc.GetString("spook-flip-lights"), ent.Owner, PopupType.Medium);
        args.Handled = true;
    }

    private void OnBurnLights(Entity<BurnLightsComponent> ent, ref BurnLightsEvent args)
    {
        var coords = Transform(args.Performer).Coordinates;
        _lights.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.SearchRadius, _lights);
        var lights = _lights.ToList();
        _random.Shuffle(lights);

        var lightBrokenCounter = 0;
        foreach (var entity in lights)
        {
            if (lightBrokenCounter > ent.Comp.MaxBurnLights)
                break;

            _poweredLight.TryDestroyBulb(entity, entity.Comp);

            coords = Transform(entity).Coordinates;
            _fireTargets.Clear();
            _lookup.GetEntitiesInRange(coords, ent.Comp.Range, _fireTargets);
            foreach (var target in _fireTargets)
            {
                if (_humanoidQuery.HasComp(target))
                    continue;

                target.Comp.FireStacks += ent.Comp.FireStack.Next(_random);
                _flammable.Ignite(target, entity, target.Comp);
            }

            lightBrokenCounter++;
        }

        _popup.PopupEntity(Loc.GetString("spook-burn-lights"), ent.Owner, PopupType.Medium);
        args.Handled = true;
    }

    private void OnOpenDoors(Entity<OpenDoorsSpookComponent> ent, ref OpenDoorsSpookEvent args)
    {
        var entities = _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.SearchRadius).ToList();
        _random.Shuffle(entities);

        var openedCounter = 0;
        foreach (var entity in entities)
        {
            if (openedCounter > ent.Comp.MaxContainer)
                break;

            if (_entityStorageQuery.HasComp(entity))
            {
                _entityStorage.OpenStorage(entity);
                openedCounter++;
                continue;
            }

            if (_doorQuery.HasComp(entity))
            {
                _door.TryOpen(entity);
                openedCounter++;
            }
        }

        args.Handled = true;
    }

    private void OnCreateSmoke(Entity<CreateSpookSmokeComponent> ent, ref CreateSmokeSpookEvent args)
    {
        // TODO make reagent that makes you drop items in smoke
        var grid = _transform.GetGrid(ent.Owner);
        var center = Transform(ent.Owner).Coordinates;
        var map = _transform.GetMap(ent.Owner);

        if (map == null || grid == null)
            return;

        for (var i = 0; i < ent.Comp.SmokeAmount; i++)
        {
            var offsetX = _random.Next(-ent.Comp.SearchRange, ent.Comp.SearchRange + 1);
            var offsetY = _random.Next(-ent.Comp.SearchRange, ent.Comp.SearchRange + 1);

            var targetCoords = new EntityCoordinates(grid.Value, center.X + offsetX, center.Y + offsetY);

            var smokeEnt = SpawnAtPosition(ent.Comp.SmokeProto, targetCoords);
            _smoke.StartSmoke(smokeEnt, ent.Comp.SmokeSolution, ent.Comp.Duration, ent.Comp.SpreadAmount);
        }

        _popup.PopupEntity(Loc.GetString("spook-create-smoke"), ent.Owner, PopupType.Medium);
        args.Handled = true;
    }

    private void OnCreateEctoplasm(Entity<CreateEctoplasmComponent> ent, ref CreateEctoplasmEvent args)
    {
        var grid = _transform.GetGrid(ent.Owner);
        var center = Transform(ent.Owner).Coordinates;
        var map = _transform.GetMap(ent.Owner);

        if (map == null || grid == null)
            return;

        // TODO: make this an entity effect and stop copy pasting ts
        var amount = _random.Next(ent.Comp.AmountMinMax.X, ent.Comp.AmountMinMax.Y + 1);
        for (var i = 0; i < amount; i++)
        {
            var offsetX = _random.Next(-ent.Comp.SearchRange, ent.Comp.SearchRange + 1);
            var offsetY = _random.Next(-ent.Comp.SearchRange, ent.Comp.SearchRange + 1);

            var targetCoords = new EntityCoordinates(grid.Value, center.X + offsetX, center.Y + offsetY);

            SpawnAtPosition(ent.Comp.EctoplasmProto, targetCoords);
        }

        _popup.PopupEntity(Loc.GetString("spook-create-ectoplasm"), ent.Owner, PopupType.Medium);
        args.Handled = true;
    }

    private void OnSapAPC(Entity<SapAPCComponent> ent, ref SapApcEvent args)
    {
        var chargeToRemove = ent.Comp.ChargeToRemove;

        if (TryComp<PassiveWraithPointsComponent>(ent.Owner, out var passiveWraithPoints))
            chargeToRemove *= (float)passiveWraithPoints.WpGeneration;

        var coords = Transform(ent).Coordinates;
        _apcs.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.SearchRange, _apcs);

        if (_apcs.Count == 0)
            return;

        var apc = _random.Pick(_apcs);
        _battery.ChangeCharge(apc.Owner, -chargeToRemove);
        _popup.PopupEntity(Loc.GetString("spook-apc-sap"), apc, PopupType.MediumCaution);

        args.Handled = true;
    }

    private void OnRandomSpook(Entity<RandomSpookComponent> ent, ref RandomSpookEvent args)
    {
        var actions = _actions.GetActions(ent.Owner).ToList();
        _random.Shuffle(actions);

        foreach (var action in actions)
        {
            if (!_actionQuery.TryComp(action, out var actionComp)
                || action == args.Action // skip itself
                || _actions.IsCooldownActive(actionComp, _timing.CurTime))
            {
                _popup.PopupEntity(Loc.GetString("spook-on-cooldown"), ent.Owner);
                continue;
            }

            _actions.PerformAction(args.Performer, action);
            _actions.StartUseDelay(action.Owner);
            break;
        }

        args.Handled = true;
    }
}
