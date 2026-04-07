using Content.Shared.Construction.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Maps;
using Content.Trauma.Shared.HolographicProjector.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.HolographicProjector.EntitySystems;

public sealed class GenericFieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly GenericFieldSystem _genericfield = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericFieldGeneratorComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ReAnchorEvent>(OnReanchorEvent);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GenericFieldGeneratorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!HasComp<BatteryComponent>(uid)
            || _timing.CurTime < comp.PowerTimer)
                continue;

            comp.PowerTimer = _timing.CurTime + comp.PowerTime;

            if (comp.IsConnected)
                _battery.UseCharge(uid, comp.PowerDrain);
        }
    }

    #region Events

    private void OnStartup(Entity<GenericFieldGeneratorComponent> ent, ref ComponentStartup args)
    {
        _signalSystem.EnsureSinkPorts(ent, ent.Comp.TogglePort, ent.Comp.OnPort, ent.Comp.OffPort);
        _signalSystem.EnsureSourcePorts(ent, ent.Comp.ConnectionStatusPort, ent.Comp.FieldConnectedPort, ent.Comp.FieldDisconnectedPort);
        ChangePowerVisualizer(ent);
        ChangeOnLightVisualizer(ent);
        UpdateConnectionLights(ent);
        ChangeConnectionLightVisualizer(ent);
        if (!ent.Comp.Enabled) return;
        TryGenerateFieldConnection(ent);
    }

    private void OnActivate(Entity<GenericFieldGeneratorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled
        || !TryComp(ent, out TransformComponent? transformComp)
        || !transformComp.Anchored)
            return;

        if (ent.Comp.Enabled)
        {
            TurnOff(ent);
        }
        else
        {
            TurnOn(ent);
        }

        args.Handled = true;
        ChangeOnLightVisualizer(ent);
        Dirty(ent, ent.Comp);
    }

    private void OnAnchorChanged(Entity<GenericFieldGeneratorComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            RemoveConnections(ent);
    }

    private void OnReanchorEvent(Entity<GenericFieldGeneratorComponent> ent, ref ReAnchorEvent args)
    {
        GridCheck(ent);
    }

    private void OnComponentRemoved(Entity<GenericFieldGeneratorComponent> ent, ref ComponentRemove args)
    {
        RemoveConnections(ent);
    }

    private void OnUnanchorAttempt(EntityUid uid, GenericFieldGeneratorComponent component, UnanchorAttemptEvent args)
    {
        if (!component.Enabled || !component.IsConnected) return;

        _popup.PopupClient(Loc.GetString("comp-genericfield-anchor-warning"), args.User, args.User, PopupType.LargeCaution);
        args.Cancel();
    }

    private void TurnOn(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (ent.Comp.ConnectedGenerator != null)
            return;

        _popup.PopupClient(Loc.GetString("comp-genericfield-turned-on"), ent, _player.LocalEntity);
        ent.Comp.Enabled = true;
        TryGenerateFieldConnection(ent);
        Dirty(ent, ent.Comp);
    }

    private void TurnOff(Entity<GenericFieldGeneratorComponent> ent)
    {
        _popup.PopupClient(Loc.GetString("comp-genericfield-turned-off"), ent, _player.LocalEntity);
        _audio.PlayLocal(ent.Comp.DeactivationSound, ent, _player.LocalEntity);
        ent.Comp.Enabled = false;
        if (ent.Comp.ConnectedGenerator is { } pair)
        {
            pair.Comp.Enabled = false;
            Dirty(pair, pair.Comp);
        }
        Dirty(ent, ent.Comp);
        RemoveConnections(ent);
    }


    /// <summary>
    /// Deletes the fields and removes the respective connections for the generators.
    /// </summary>
    private void RemoveConnections(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (ent.Comp.ConnectedGenerator is not { } connectedGenerator)
            return;

        ent.Comp.ConnectedGenerator = null;
        connectedGenerator.Comp.ConnectedGenerator = null;
        ent.Comp.IsConnected = false;
        connectedGenerator.Comp.IsConnected = false;

        foreach (var field in ent.Comp.ConnectedFields)
        {
            if (TryComp<GenericFieldComponent>(field, out var fieldComp) && fieldComp.TempTile)
                _genericfield.TempTileCleanup((field, fieldComp));
            QueueDel(field);
        }

        if (HasComp<DeviceLinkSourceComponent>(ent))
        {
            _signalSystem.SendSignal(ent, ent.Comp.ConnectionStatusPort, false);
            _signalSystem.InvokePort(ent, ent.Comp.FieldDisconnectedPort);
        }

        if (HasComp<DeviceLinkSourceComponent>(connectedGenerator))
        {
            _signalSystem.SendSignal(connectedGenerator, connectedGenerator.Comp.ConnectionStatusPort, false);
            _signalSystem.InvokePort(connectedGenerator, connectedGenerator.Comp.FieldDisconnectedPort);
        }


        if (ent.Comp.IsConnected)
            _popup.PopupClient(Loc.GetString("comp-genericfield-disconnected"), ent, _player.LocalEntity, PopupType.LargeCaution);

        if (connectedGenerator.Comp.IsConnected)
            _popup.PopupClient(Loc.GetString("comp-genericfield-disconnected"), connectedGenerator, _player.LocalEntity, PopupType.LargeCaution);

        ChangeConnectionLightVisualizer(connectedGenerator);
        UpdateConnectionLights(connectedGenerator);
        ChangeConnectionLightVisualizer(ent);
        UpdateConnectionLights(ent);
        Dirty(ent, ent.Comp);
    }

    private void OnBatteryStateChanged(Entity<GenericFieldGeneratorComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.OldState != BatteryState.Empty && args.NewState == BatteryState.Empty && ent.Comp.Charged)
        {
            ent.Comp.Charged = false;
            TurnOff(ent);
        }
        else if (args.OldState != BatteryState.Full && args.NewState == BatteryState.Full && !ent.Comp.Charged)
        {
            ent.Comp.Charged = true;
            if (ent.Comp.Enabled) // If it's on, try to connect it
                TryGenerateFieldConnection(ent);
        }
        Dirty(ent, ent.Comp);
    }

    private void OnSignalReceived(Entity<GenericFieldGeneratorComponent> ent, ref SignalReceivedEvent args) //basic signal compatability
    {
        if (!TryComp(ent, out TransformComponent? transformComp)
        || !transformComp.Anchored)
            return;

        if (args.Port == ent.Comp.OnPort) // This is kinda evil but eh
        {
            TurnOn(ent);
        }
        if (args.Port == ent.Comp.OffPort)
        {
            TurnOff(ent);
        }
        if (args.Port == ent.Comp.TogglePort)
        {
            if (!ent.Comp.Enabled)
            {
                TurnOn(ent);
            }
            else
            {
                TurnOff(ent);
            }
        }
        ChangeOnLightVisualizer(ent);
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Helper called by fields when destroyed
    /// </summary>
    /// <param name="ent"></param>
    public void FieldDestroyed(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (ent.Comp.ConnectedGenerator is not { } pair)
            return;

        if (TryComp<BatteryComponent>(ent, out var batteryComponent))
            _battery.UseCharge(ent.Owner, batteryComponent.MaxCharge);

        if (TryComp<BatteryComponent>(pair, out var pairBatteryComponent))
            _battery.UseCharge(pair.Owner, pairBatteryComponent.MaxCharge);

        RemoveConnections(ent);
        _audio.PlayLocal(ent.Comp.DeactivationSound, ent, _player.LocalEntity);
    }

    private void OnChargeChanged(Entity<GenericFieldGeneratorComponent> ent, ref ChargeChangedEvent args)
    {
        ChangePowerVisualizer(ent);
    }

    #endregion

    #region Connections

    /// <summary>
    /// This will attempt to establish a connection of fields between two generators.
    /// If all the checks pass and fields spawn, it will store this connection on each respective ent.
    /// </summary>
    private void TryGenerateFieldConnection(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (!ent.Comp.Enabled
        || !ent.Comp.Charged
        || !Transform(ent).Anchored)
            return;

        var (worldPosition, worldRotation) = _transformSystem.GetWorldPositionRotation(Transform(ent));
        var dirRad = worldRotation - Angle.FromDegrees(90);

        var ray = new CollisionRay(worldPosition, dirRad.ToVec(), ent.Comp.CollisionMask);
        var rayCastResults = _physics.IntersectRay(Transform(ent).MapID, ray, ent.Comp.MaxLength, ent, false);
        var genQuery = GetEntityQuery<GenericFieldGeneratorComponent>();

        RayCastResults? closestResult = null;

        foreach (var result in rayCastResults)
        {
            if (genQuery.HasComponent(result.HitEntity))
                closestResult = result;

            break;
        }
        if (closestResult == null)
            return;

        var target = closestResult.Value.HitEntity;

        if (!TryComp<GenericFieldGeneratorComponent>(target, out var otherFieldGeneratorComponent)
        || !Transform(target).Anchored // Is the target anchored?
        || Transform(ent).GridUid != Transform(target).GridUid // Are the generators on the same grid?
        || otherFieldGeneratorComponent.CreatedField != ent.Comp.CreatedField // Are the generators creating the same type of field?
        || Transform(ent).LocalRotation.GetCardinalDir() != Transform(target).LocalRotation.GetCardinalDir().GetOpposite()) // Are the generators facing eachother?
        {
            return;
        }

        var otherFieldGenerator = (target, otherFieldGeneratorComponent);
        ent.Comp.ConnectedGenerator = otherFieldGenerator;
        otherFieldGeneratorComponent.ConnectedGenerator = otherFieldGenerator;

        var fields = GenerateFieldConnection(ent, otherFieldGenerator);

        ent.Comp.ConnectedFields = fields;
        otherFieldGeneratorComponent.ConnectedFields = fields;

        if (!ent.Comp.IsConnected)
        {
            ent.Comp.Enabled = true;
            ent.Comp.IsConnected = true;
            ChangeConnectionLightVisualizer(ent);
            UpdateConnectionLights(ent);
        }

        if (!otherFieldGeneratorComponent.IsConnected)
        {
            otherFieldGeneratorComponent.IsConnected = true;
            ChangeConnectionLightVisualizer(otherFieldGenerator);
            UpdateConnectionLights(otherFieldGenerator);
        }

        _popup.PopupClient(Loc.GetString("comp-genericfield-connected"), ent, _player.LocalEntity);
        _popup.PopupClient(Loc.GetString("comp-genericfield-connected"), target, _player.LocalEntity);
        _audio.PlayLocal(ent.Comp.ActivationSound, ent, _player.LocalEntity);
        Dirty(ent, ent.Comp);
        Dirty(target, otherFieldGeneratorComponent);
        return;
    }

    /// <summary>
    /// Spawns fields between two generators if the <see cref="TryGenerateFieldConnection"/> finds two generators to connect.
    /// </summary>
    /// <param name="firstGen">The source field ent</param>
    /// <param name="secondGen">The second ent that the source is connected to</param>
    private List<EntityUid> GenerateFieldConnection(Entity<GenericFieldGeneratorComponent> firstGen, Entity<GenericFieldGeneratorComponent> secondGen)
    {
        if (HasComp<DeviceLinkSourceComponent>(firstGen))
        {
            _signalSystem.SendSignal(firstGen, firstGen.Comp.ConnectionStatusPort, true);
            _signalSystem.InvokePort(firstGen, firstGen.Comp.FieldConnectedPort);
        }

        if (HasComp<DeviceLinkSourceComponent>(secondGen))
        {
            _signalSystem.SendSignal(secondGen, secondGen.Comp.ConnectionStatusPort, true);
            _signalSystem.InvokePort(secondGen, secondGen.Comp.FieldConnectedPort);
        }

        var fieldList = new List<EntityUid>();
        var gen1Coords = Transform(firstGen).Coordinates;
        var gen2Coords = Transform(secondGen).Coordinates;

        var delta = gen2Coords.Position - gen1Coords.Position;
        var dirVec = delta.Normalized();
        var stopDist = delta.Length();
        var currentOffset = dirVec;

        while (currentOffset.Length() < stopDist)
        {
            var currentCoords = gen1Coords.Offset(currentOffset);
            var newField = PredictedSpawnAtPosition(firstGen.Comp.CreatedField, currentCoords);

            var xform = Transform(newField);
            xform.LocalRotation = dirVec.ToAngle() + Math.PI / 2;
            fieldList.Add(newField);
            currentOffset += dirVec;
            if (TryComp<GenericFieldComponent>(newField, out var fieldComp))
            {
                fieldComp.SourceGen = firstGen;
                if (_transformSystem.AnchorEntity(newField)) continue; //check if entity can anchor normally first

                if (!_tiledef.TryGetDefinition("HolographicTile", out var tileDef))
                    break;

                if (Transform(firstGen).GridUid is not { } gridUid
                || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
                    break;

                var tile = _mapSystem.GetTileRef(gridUid, mapGrid, _transformSystem.GetMapCoordinates(newField, xform));
                _tile.ReplaceTile(tile, (ContentTileDefinition) tileDef, gridUid, mapGrid);
                fieldComp.TempTile = true;
                Dirty(newField, fieldComp);

                if (!_transformSystem.AnchorEntity(newField)) // if this fails to anchor, something has gone horribly wrong
                    RemoveConnections(firstGen); //remove connection and so it can try again
            }
        }
        return fieldList;
    }

    /// <summary>
    /// Creates a light component for the spawned fields.
    /// </summary>
    public void UpdateConnectionLights(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (_light.TryGetLight(ent, out var pointLightComponent))
            _light.SetEnabled(ent, ent.Comp.IsConnected, pointLightComponent);
    }

    /// <summary>
    /// Checks to see if this or the other gens connected to a new grid. If they did, remove connection.
    /// </summary>
    public void GridCheck(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (ent.Comp.ConnectedGenerator == null)
            return;

        var xFormQuery = GetEntityQuery<TransformComponent>();

        var gen1ParentGrid = xFormQuery.GetComponent(ent).GridUid;
        var gent2ParentGrid = xFormQuery.GetComponent(ent.Comp.ConnectedGenerator.Value).GridUid;

        if (gen1ParentGrid != gent2ParentGrid)
            RemoveConnections(ent);
    }

    #endregion

    // Entered: coal mines
    #region VisualizerHelpers

    /// <summary>
    /// Check if a fields power falls between certain ranges to update the field gen visual for power.
    /// </summary>
    private void ChangePowerVisualizer(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (!TryComp<BatteryComponent>(ent, out var batteryComponent))
            return;
        var charge = batteryComponent.LastCharge;
        _appearance.SetData(ent, GenericFieldGeneratorVisuals.PowerLight, charge switch //I dont like hardcoding these values, but I also dont feel like having a giant pile of if statments
        {
            <= 50 => PowerLevelVisuals.NoPower,
            >= 1450 => PowerLevelVisuals.FullPower,
            >= 1200 => PowerLevelVisuals.VeryHighPower,
            >= 900 => PowerLevelVisuals.HighPower,
            >= 600 => PowerLevelVisuals.MediumPower,
            >= 300 => PowerLevelVisuals.LowPower,
            _ => PowerLevelVisuals.MinimalPower
        });
    }

    private void ChangeConnectionLightVisualizer(Entity<GenericFieldGeneratorComponent> ent)
    {
        _appearance.SetData(ent, GenericFieldGeneratorVisuals.ConnectionLight, ent.Comp.IsConnected);
    }

    private void ChangeOnLightVisualizer(Entity<GenericFieldGeneratorComponent> ent)
    {
        _appearance.SetData(ent, GenericFieldGeneratorVisuals.OnLight, ent.Comp.Enabled);
    }
    #endregion
}
