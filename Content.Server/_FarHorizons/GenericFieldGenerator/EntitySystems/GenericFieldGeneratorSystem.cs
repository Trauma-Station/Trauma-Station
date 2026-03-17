using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared._FarHorizons.GenericFieldGenerator.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;

namespace Content.Server._FarHorizons.GenericFieldGenerator.EntitySystems;

public sealed class GenericFieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AppearanceSystem _visualizer = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly GenericFieldSystem _genericfield = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericFieldGeneratorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ReAnchorEvent>(OnReanchorEvent);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GenericFieldGeneratorComponent>();
        while (query.MoveNext(out var uid, out var generator))
        {
            generator.Accumulator += frameTime;

            if (!(generator.Accumulator >= 0.2f))
                continue;
            generator.Accumulator -= 0.2f;
                
            if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
                continue;

            if (generator.IsConnected)
                //Drain the internal battery
                _battery.UseCharge(uid, generator.PowerDrain);
            
            else if (batteryComponent.MaxCharge <= batteryComponent.LastCharge)
            //try to connect again if not connected and fully charged. might be a bad idea for performance, but I want having both connected to power for redundancy to be viable.
            {
                generator.RetryWait += 1;
                if (generator.RetryWait >= 5)
                    TurnOn((uid, generator));
            }
        }
    }

    #region Events

    private void OnMapInit(Entity<GenericFieldGeneratorComponent> generator, ref MapInitEvent args)
    {
        if (TryComp<PowerNetworkBatteryComponent>(generator, out var batteryComponent))
        {
            batteryComponent.MaxChargeRate = generator.Comp.Enabled ? generator.Comp.ChargeRate : 0;
        }
        ChangePowerVisualizer(generator);
        ChangeOnLightVisualizer(generator);
        UpdateConnectionLights(generator);
        ChangeConnectionLightVisualizer(generator);
        _signalSystem.EnsureSinkPorts(generator, generator.Comp.TogglePort, generator.Comp.OnPort, generator.Comp.OffPort);
        _signalSystem.EnsureSourcePorts(generator, generator.Comp.ConnectionStatusPort, generator.Comp.FieldConnectedPort, generator.Comp.FieldDisconnectedPort);
    }
    private void OnExamine(EntityUid uid, GenericFieldGeneratorComponent component, ExaminedEvent args)
    {
        if (component.Enabled)
            args.PushMarkup(Loc.GetString("comp-genericfield-on"));

        else
            args.PushMarkup(Loc.GetString("comp-genericfield-off"));
    }

    private void OnActivate(Entity<GenericFieldGeneratorComponent> generator, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(generator, out TransformComponent? transformComp) && transformComp.Anchored &&
            TryComp<PowerNetworkBatteryComponent>(generator, out var batteryComponent))
        {
            if (!generator.Comp.Enabled)
            {
                //TurnOn
                generator.Comp.Enabled = true;
                batteryComponent.MaxChargeRate = generator.Comp.ChargeRate;
                _popupSystem.PopupEntity(Loc.GetString("comp-genericfield-turned-on"), generator);
            }
            else
            {
                //TurnOff
                generator.Comp.Enabled = false;
                batteryComponent.MaxChargeRate = 0;
                _popupSystem.PopupEntity(Loc.GetString("comp-genericfield-turned-off"), generator);
            }
            ChangeOnLightVisualizer(generator);
        }
        args.Handled = true;
    }

    private void OnAnchorChanged(Entity<GenericFieldGeneratorComponent> generator, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            RemoveConnections(generator);
    }

    private void OnReanchorEvent(Entity<GenericFieldGeneratorComponent> generator, ref ReAnchorEvent args) => GridCheck(generator);

    private void OnUnanchorAttempt(EntityUid uid, GenericFieldGeneratorComponent component, UnanchorAttemptEvent args)
    {
        if (component.Enabled || component.IsConnected)
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-genericfield-anchor-warning"), args.User, args.User, PopupType.LargeCaution);
            args.Cancel();
        }
    }

    private void TurnOn(Entity<GenericFieldGeneratorComponent> generator)
    {
        var component = generator.Comp;
        var genXForm = Transform(generator);
        generator.Comp.Charged = true;

        if (component.Connections != null)
            return; // Already has an active connection

        TryGenerateFieldConnection(generator, genXForm);
    }

    private void TurnOff(Entity<GenericFieldGeneratorComponent> generator)
    {
        generator.Comp.Charged = false;
        RemoveConnections(generator);
    }

    private void OnComponentRemoved(Entity<GenericFieldGeneratorComponent> generator, ref ComponentRemove args) => RemoveConnections(generator);

    /// <summary>
    /// Deletes the fields and removes the respective connections for the generators.
    /// </summary>
    private void RemoveConnections(Entity<GenericFieldGeneratorComponent> generator)
    {
        if (TryComp<DeviceLinkSourceComponent>(generator, out _))
        {
        _signalSystem.SendSignal(generator, generator.Comp.ConnectionStatusPort, false);
        _signalSystem.InvokePort(generator, generator.Comp.FieldDisconnectedPort);
        }

        var (uid, component) = generator;

        if (component.Connections == null)
            return;

        var value = component.Connections.Value;
            
        foreach (var field in value.Item2)
        {
            if (TryComp<GenericFieldComponent>(field, out var fieldComp))
                _genericfield.TempTileCleanup((field, fieldComp));
            QueueDel(field);
        }

        value.Item1.Comp.Connections = null;

        if (TryComp<DeviceLinkSourceComponent>(value.Item1, out _))
        {
            _signalSystem.SendSignal(value.Item1, generator.Comp.ConnectionStatusPort, false);
            _signalSystem.InvokePort(value.Item1, generator.Comp.FieldDisconnectedPort);
        }

        value.Item1.Comp.IsConnected = false;
        ChangeConnectionLightVisualizer(value.Item1);
        UpdateConnectionLights(value.Item1);

        component.Connections = null;

        if (component.IsConnected)
            _popupSystem.PopupEntity(Loc.GetString("comp-genericfield-disconnected"), uid, PopupType.LargeCaution);
        component.IsConnected = false;
        ChangeConnectionLightVisualizer(generator);
        UpdateConnectionLights(generator);
        _adminLogger.Add(LogType.FieldGeneration, LogImpact.Medium, $"{ToPrettyString(uid)} lost field connections"); // Ideally LogImpact would depend on if there is a singulo nearby
    }

    private void OnBatteryStateChanged(Entity<GenericFieldGeneratorComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.OldState != BatteryState.Empty && args.NewState == BatteryState.Empty && ent.Comp.Charged) //Checks if already charged to stop repeated activation when changing states rapidly
        {
            TurnOff(ent);
        }
        if (args.OldState != BatteryState.Full && args.NewState == BatteryState.Full && (!ent.Comp.Charged || !ent.Comp.IsConnected)) // also checks if not connected yet
        {
            TurnOn(ent);
        }
    }

    private void OnSignalReceived(Entity<GenericFieldGeneratorComponent> generator, ref SignalReceivedEvent args) //basic signal compatability
    {
        if (TryComp(generator, out TransformComponent? transformComp) && transformComp.Anchored)
        {
            if (TryComp<PowerNetworkBatteryComponent>(generator, out var batteryComponent))
            {
                if (args.Port == generator.Comp.OnPort)
                {
                    //TurnOn
                    generator.Comp.Enabled = true;
                    batteryComponent.MaxChargeRate = generator.Comp.ChargeRate;
                    _popupSystem.PopupEntity(Loc.GetString("comp-genericfield-turned-on"), generator);
                }
                if (args.Port == generator.Comp.OffPort)
                {
                    //TurnOff
                    generator.Comp.Enabled = false;
                    batteryComponent.MaxChargeRate = 0;
                    _popupSystem.PopupEntity(Loc.GetString("comp-genericfield-turned-off"), generator);
                }
                if (args.Port == generator.Comp.TogglePort) // Toggle
                {
                    if (!generator.Comp.Enabled)
                    {
                        //TurnOn
                        generator.Comp.Enabled = true;
                        batteryComponent.MaxChargeRate = generator.Comp.ChargeRate;
                        _popupSystem.PopupEntity(Loc.GetString("comp-genericfield-turned-on"), generator);
                    }
                    else
                    {
                        //TurnOff
                        generator.Comp.Enabled = false;
                        batteryComponent.MaxChargeRate = 0;
                        _popupSystem.PopupEntity(Loc.GetString("comp-genericfield-turned-off"), generator);
                    }
                }
            }
            ChangeOnLightVisualizer(generator);
        }
    }

    /// <summary>
    /// Helper called by fields when destroyed
    /// </summary>
    /// <param name="generator"></param>
    public void FieldDestroyed(Entity<GenericFieldGeneratorComponent> generator)
    {
        if (TryComp<BatteryComponent>(generator, out var batteryComponent))
        {
            _battery.UseCharge(generator.Owner, batteryComponent.MaxCharge);
        }
        _adminLogger.Add(LogType.FieldGeneration, LogImpact.High, $"{ToPrettyString(generator)} had a field destroyed"); //fields dont break randomly, usually antag activity
        RemoveConnections(generator);
    }

    private void OnChargeChanged(Entity<GenericFieldGeneratorComponent> generator, ref ChargeChangedEvent args) => ChangePowerVisualizer(generator);

    #endregion

    #region Connections

    /// <summary>
    /// This will attempt to establish a connection of fields between two generators.
    /// If all the checks pass and fields spawn, it will store this connection on each respective generator.
    /// </summary>
    /// <param name="generator">The field generator component</param>
    /// <param name="gen1XForm">The transform component for the first generator</param>
    /// <returns></returns>
    private bool TryGenerateFieldConnection(Entity<GenericFieldGeneratorComponent> generator, TransformComponent gen1XForm)
    {
        var component = generator.Comp;
        component.RetryWait = 0; //reset wait time after trying
        if (!component.Enabled)
            return false;

        if (!gen1XForm.Anchored)
            return false;

        var (worldPosition, worldRotation) = _transformSystem.GetWorldPositionRotation(gen1XForm);
        var dirRad = worldRotation - Angle.FromDegrees(90d); //needs to be like this for the raycast to work properly; changed to just use World Rotation and a fixed value

        var ray = new CollisionRay(worldPosition, dirRad.ToVec(), component.CollisionMask);
        var rayCastResults = _physics.IntersectRay(gen1XForm.MapID, ray, component.MaxLength, generator, false);
        var genQuery = GetEntityQuery<GenericFieldGeneratorComponent>();

        RayCastResults? closestResult = null;

        foreach (var result in rayCastResults)
        {
            if (genQuery.HasComponent(result.HitEntity))
                closestResult = result;

            break;
        }
        if (closestResult == null)
            return false;

        var ent = closestResult.Value.HitEntity;

        if (!TryComp<GenericFieldGeneratorComponent>(ent, out var otherFieldGeneratorComponent) ||
            otherFieldGeneratorComponent == component ||
            !TryComp<PhysicsComponent>(ent, out var collidableComponent) ||
            collidableComponent.BodyType != BodyType.Static ||
            gen1XForm.ParentUid != Transform(ent).ParentUid)
        {
            return false;
        }

        if(otherFieldGeneratorComponent.CreatedField != component.CreatedField) // check if other generator generates the same type of field
        {
            return false;
        }

        if(Transform(ent).LocalRotation.GetCardinalDir() != gen1XForm.LocalRotation.GetCardinalDir().GetOpposite()) // Both Generators facing opposite directions? works, dont touch it
        {
            return false;
        }

        var otherFieldGenerator = (ent, otherFieldGeneratorComponent);
        var fields = GenerateFieldConnection(generator, otherFieldGenerator);

        component.Connections = (otherFieldGenerator, fields);
        otherFieldGeneratorComponent.Connections = (generator, fields);

        if (!component.IsConnected)
        {
            component.IsConnected = true;
            ChangeConnectionLightVisualizer(generator);
            UpdateConnectionLights(generator);
        }

        if (!otherFieldGeneratorComponent.IsConnected)
        {
            otherFieldGeneratorComponent.IsConnected = true;
            ChangeConnectionLightVisualizer(otherFieldGenerator);
            UpdateConnectionLights(otherFieldGenerator);
        }

        _popupSystem.PopupEntity(Loc.GetString("comp-genericfield-connected"), generator);
        return true;
    }

    /// <summary>
    /// Spawns fields between two generators if the <see cref="TryGenerateFieldConnection"/> finds two generators to connect.
    /// </summary>
    /// <param name="firstGen">The source field generator</param>
    /// <param name="secondGen">The second generator that the source is connected to</param>
    /// <returns></returns>
    private List<EntityUid> GenerateFieldConnection(Entity<GenericFieldGeneratorComponent> firstGen, Entity<GenericFieldGeneratorComponent> secondGen)
    {
        if (TryComp<DeviceLinkSourceComponent>(firstGen, out _))
        {
            _signalSystem.SendSignal(firstGen, firstGen.Comp.ConnectionStatusPort, true);
            _signalSystem.SendSignal(secondGen, secondGen.Comp.ConnectionStatusPort, true);
            _signalSystem.InvokePort(firstGen, firstGen.Comp.FieldConnectedPort);
            _signalSystem.InvokePort(secondGen, secondGen.Comp.FieldConnectedPort);
        }

        var fieldList = new List<EntityUid>();
        var gen1Coords = Transform(firstGen).Coordinates;
        var gen2Coords = Transform(secondGen).Coordinates;

        var delta = (gen2Coords - gen1Coords).Position;
        var dirVec = delta.Normalized();
        var stopDist = delta.Length();
        var currentOffset = dirVec;
        while (currentOffset.Length() < stopDist)
        {
            var currentCoords = gen1Coords.Offset(currentOffset);
            var newField = Spawn(firstGen.Comp.CreatedField, currentCoords);

            var fieldXForm = Transform(newField);
            _transformSystem.SetParent(newField, fieldXForm, firstGen);
            if (dirVec.GetDir() == Direction.East || dirVec.GetDir() == Direction.West)
            {
                var angle = fieldXForm.LocalPosition.ToAngle();
                var rotateBy90 = angle.Degrees + 90;
                var rotatedAngle = Angle.FromDegrees(rotateBy90);

                fieldXForm.LocalRotation = rotatedAngle;
            }
            fieldList.Add(newField);
            currentOffset += dirVec;
            if (TryComp<GenericFieldComponent>(newField, out var fieldComp))
            {
                fieldComp.SourceGen = firstGen;
                if (!_transformSystem.AnchorEntity(newField)) //check if entity can anchor normally first
                {
                    if (!_tiledef.TryGetDefinition("HolographicTile", out var tileDef))
                        break;

                    var gridUid = Transform(firstGen).ParentUid;
                    fieldComp.GridUid = gridUid;

                    if (!TryComp<MapGridComponent>(gridUid, out var mapGrid)) 
                        break;

                    fieldComp.MapGrid = mapGrid;

                    var tile = _mapSystem.GetTileRef(gridUid, mapGrid, _transformSystem.GetMapCoordinates(newField, fieldXForm));
                    fieldComp.Tileref = tile; //GenericFieldComponent needs to know what tile it is

                    _tile.ReplaceTile(tile, (ContentTileDefinition)tileDef, gridUid, mapGrid);
                    fieldComp.TempTile = true;
                    if (!_transformSystem.AnchorEntity(newField)) // if this fails to anchor, something has gone horribly wrong
                        RemoveConnections(firstGen); //remove connection and so it can try again
                }
            }  
        }
        return fieldList;
    }

    /// <summary>
    /// Creates a light component for the spawned fields.
    /// </summary>
    public void UpdateConnectionLights(Entity<GenericFieldGeneratorComponent> generator)
    {
        if (_light.TryGetLight(generator, out var pointLightComponent))
        {
            _light.SetEnabled(generator, generator.Comp.IsConnected, pointLightComponent);
        }
    }

    /// <summary>
    /// Checks to see if this or the other gens connected to a new grid. If they did, remove connection.
    /// </summary>
    public void GridCheck(Entity<GenericFieldGeneratorComponent> generator)
    {
        if (generator.Comp.Connections == null)
            return;
        
        var xFormQuery = GetEntityQuery<TransformComponent>();

        var gen1ParentGrid = xFormQuery.GetComponent(generator).ParentUid;
        var gent2ParentGrid = xFormQuery.GetComponent(generator.Comp.Connections.Value.Item1).ParentUid;

        if (gen1ParentGrid != gent2ParentGrid)
            RemoveConnections(generator);
    }

    #endregion

    #region VisualizerHelpers

    /// <summary>
    /// Check if a fields power falls between certain ranges to update the field gen visual for power.
    /// </summary>
    /// <param name="generator"></param>
    private void ChangePowerVisualizer(Entity<GenericFieldGeneratorComponent> generator)
    {
        if (!TryComp<BatteryComponent>(generator, out var batteryComponent))
            return;
        var charge = batteryComponent.LastCharge;
        _visualizer.SetData(generator, GenericFieldGeneratorVisuals.PowerLight, charge switch //I dont like hardcoding these values, but I also dont feel like having a giant pile of if statments
        {
            <= 50 => PowerLevelVisuals.NoPower,
            >= 1450 => PowerLevelVisuals.FullPower,
            >= 1200 => PowerLevelVisuals.VeryHighPower,
            >= 900 => PowerLevelVisuals.HighPower,
            >= 600 => PowerLevelVisuals.MediumPower,
            >= 300 => PowerLevelVisuals.LowPower,
            _ => PowerLevelVisuals.MinimalPower
        });
        if (TryComp<BatteryChargerComponent>(generator, out _))
            _visualizer.SetData(generator, GenericFieldGeneratorVisuals.ChargeLight, CheckHasPower<BatteryChargerComponent>(generator.Owner));
    }

    bool CheckHasPower<TComp>(EntityUid entity) where TComp : BasePowerNetComponent // Taken from BatteryInterfaceSystem
    {
        if (!TryComp(entity, out TComp? comp))
            return false;

        if (comp.Net == null)
            return false;

        return comp.Net.NetworkNode.LastCombinedMaxSupply > 0;
    }
        
    private void ChangeConnectionLightVisualizer(Entity<GenericFieldGeneratorComponent> generator) => _visualizer.SetData(generator, GenericFieldGeneratorVisuals.ConnectionLight, generator.Comp.IsConnected);

    private void ChangeOnLightVisualizer(Entity<GenericFieldGeneratorComponent> generator) => _visualizer.SetData(generator, GenericFieldGeneratorVisuals.OnLight, generator.Comp.Enabled);
    #endregion
}
