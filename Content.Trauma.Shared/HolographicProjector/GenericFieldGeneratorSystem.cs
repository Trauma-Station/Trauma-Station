// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Construction.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.HolographicProjector;

public sealed partial class GenericFieldGeneratorSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedDeviceLinkSystem _signal = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<GenericFieldGeneratorComponent> _genQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericFieldGeneratorComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ReAnchorEvent>(OnReanchorEvent);
        SubscribeLocalEvent<ActiveFieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<GenericFieldGeneratorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveFieldGeneratorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.ReconnectTimer)
                continue;

            comp.ReconnectTimer = _timing.CurTime + comp.ReconnectTime;
            if (!_genQuery.TryComp(uid, out var generatorComp)
            || generatorComp.IsConnected)
                continue;
            TryGenerateFieldConnection((uid, generatorComp));
        }
    }

    #region Events

    private void OnStartup(Entity<GenericFieldGeneratorComponent> ent, ref ComponentStartup args)
    {
        _signal.EnsureSinkPorts(ent, ent.Comp.TogglePort, ent.Comp.OnPort, ent.Comp.OffPort);
        _signal.EnsureSourcePorts(ent, ent.Comp.ConnectionStatusPort, ent.Comp.FieldConnectedPort, ent.Comp.FieldDisconnectedPort);
        ChangePowerVisualizer(ent);
        ChangeOnLightVisualizer(ent);
        UpdateConnectionLights(ent);
        ChangeConnectionLightVisualizer(ent);
    }

    private void OnActivate(Entity<GenericFieldGeneratorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled
        || !TryComp(ent, out TransformComponent? transformComp)
        || !transformComp.Anchored)
            return;

        ToggleGenerator(ent, args.User);

        args.Handled = true;
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

    private void OnUnanchorAttempt(Entity<ActiveFieldGeneratorComponent> ent, ref UnanchorAttemptEvent args)
    {
        _popup.PopupPredicted(Loc.GetString("comp-genericfield-anchor-warning"), args.User, args.User, PopupType.LargeCaution);
        args.Cancel();
    }

    private void TurnOn(Entity<GenericFieldGeneratorComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.ConnectedGenerator != null)
            return;

        _popup.PopupPredicted(Loc.GetString("comp-genericfield-turned-on"), ent, user);
        ent.Comp.Enabled = true;
        EnsureComp<ActiveFieldGeneratorComponent>(ent);
        TryGenerateFieldConnection(ent, user);
        ChangeOnLightVisualizer(ent);
        Dirty(ent, ent.Comp);
    }

    private void TurnOff(Entity<GenericFieldGeneratorComponent> ent, EntityUid? user = null)
    {
        _popup.PopupPredicted(Loc.GetString("comp-genericfield-turned-off"), ent, user);
        ent.Comp.Enabled = false;
        RemComp<ActiveFieldGeneratorComponent>(ent);
        RemoveConnections(ent, user);
        ChangeOnLightVisualizer(ent);
        Dirty(ent, ent.Comp);
    }

    private void ToggleGenerator(Entity<GenericFieldGeneratorComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.Enabled)
        {
            TurnOff(ent, user);
        }
        else
        {
            TurnOn(ent, user);
        }
    }

    private void OnBatteryStateChanged(Entity<GenericFieldGeneratorComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.OldState != BatteryState.Empty && args.NewState == BatteryState.Empty && ent.Comp.Charged)
        {
            ent.Comp.Charged = false;
            RemoveConnections(ent);

            if (ent.Comp.ConnectedGenerator is not { } pair
            || !TryComp<BatteryComponent>(pair, out var pairBattery))
                return;

            _battery.UseCharge(pair.Owner, pairBattery.MaxCharge); // Fully discharge the other battery too
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
        if (!Transform(ent).Anchored)
            return;

        if (args.Port == ent.Comp.OnPort) // This is kinda evil but eh
        {
            TurnOn(ent);
        }
        else if (args.Port == ent.Comp.OffPort)
        {
            TurnOff(ent);
        }
        else if (args.Port == ent.Comp.TogglePort)
        {
            ToggleGenerator(ent);
        }
        ChangeOnLightVisualizer(ent);
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Helper called by fields when destroyed
    /// </summary>
    /// <param name="ent"></param>
    public void FieldDestroyed(Entity<GenericFieldGeneratorComponent?> ent)
    {
        if (!_genQuery.Resolve(ent, ref ent.Comp) || ent.Comp.ConnectedGenerator is not { } pair)
            return;

        if (TryComp<BatteryComponent>(ent, out var battery))
            _battery.UseCharge((ent.Owner, battery), battery.MaxCharge); // Batery being drained disables the field anyway so we don't call it again.

        if (TryComp<BatteryComponent>(pair, out battery))
            _battery.UseCharge((pair.Owner, battery), battery.MaxCharge);
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
    private void TryGenerateFieldConnection(Entity<GenericFieldGeneratorComponent> ent, EntityUid? user = null)
    {
        if (!ent.Comp.Enabled
        || !ent.Comp.Charged
        || !Transform(ent).Anchored
        || ent.Comp.IsConnected)
            return;

        var (worldPosition, worldRotation) = _xform.GetWorldPositionRotation(Transform(ent));
        var dirRad = worldRotation - Angle.FromDegrees(90);

        var ray = new CollisionRay(worldPosition, dirRad.ToVec(), ent.Comp.CollisionMask);
        var rayCastResults = _physics.IntersectRay(Transform(ent).MapID, ray, ent.Comp.MaxLength, ent, false);

        RayCastResults? closestResult = null;

        foreach (var result in rayCastResults)
        {
            if (_genQuery.HasComponent(result.HitEntity))
                closestResult = result;

            break;
        }
        if (closestResult == null)
            return;

        var pair = closestResult.Value.HitEntity;

        if (!_genQuery.TryComp(pair, out var pairComp)
        || !pairComp.Enabled
        || !pairComp.Charged
        || !Transform(pair).Anchored // Is the target anchored?
        || Transform(ent).GridUid != Transform(pair).GridUid // Are the generators on the same grid?
        || pairComp.CreatedField != ent.Comp.CreatedField // Are the generators creating the same type of field?
        || Transform(ent).LocalRotation.GetCardinalDir() != Transform(pair).LocalRotation.GetCardinalDir().GetOpposite()) // Are the generators facing eachother?
        {
            return;
        }

        ent.Comp.ConnectedGenerator = (pair, pairComp);
        pairComp.ConnectedGenerator = ent;

        var fields = GenerateFieldConnection(ent, (pair, pairComp));

        ent.Comp.ConnectedFields = fields;
        pairComp.ConnectedFields = fields;

        SetWorkingState(ent, true, user);
        SetWorkingState((pair, pairComp), true, user);
        return;
    }

    /// <summary>
    /// Spawns fields between two generators if the <see cref="TryGenerateFieldConnection"/> finds two generators to connect.
    /// </summary>
    /// <param name="firstGen">The source field ent</param>
    /// <param name="secondGen">The second ent that the source is connected to</param>
    private List<EntityUid> GenerateFieldConnection(Entity<GenericFieldGeneratorComponent> firstGen, Entity<GenericFieldGeneratorComponent> secondGen)
    {
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
            _xform.SetParent(newField, xform, firstGen);
            _physics.TrySetBodyType(newField, BodyType.Static, xform: xform); // Changing parent sets it to dynamic for some reason. Using this and not anchoring because this also works off-grid.
            xform.LocalRotation = 0; // Same rotation as parent
            fieldList.Add(newField);
            currentOffset += dirVec;

            if (!TryComp<GenericFieldComponent>(newField, out var fieldComp))
                continue;
            fieldComp.SourceGen = firstGen;
            Dirty(newField, fieldComp);
        }
        return fieldList;
    }

    /// <summary>
    /// Deletes the fields and removes the respective connections for the generators.
    /// </summary>
    private void RemoveConnections(Entity<GenericFieldGeneratorComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.ConnectedGenerator is not { } pair)
            return;

        ent.Comp.ConnectedGenerator = null;
        pair.Comp.ConnectedGenerator = null;

        foreach (var field in ent.Comp.ConnectedFields)
        {
            QueueDel(field);
        }

        SetWorkingState(ent, false, user);
        SetWorkingState(pair, false, user);
    }

    /// <summary>
    /// Updates a bunch of values when a field is created/destroyed.
    /// </summary>
    private void SetWorkingState(Entity<GenericFieldGeneratorComponent> ent, bool state, EntityUid? user = null)
    {
        if (TryComp<PowerStateComponent>(ent, out var stateComp)) stateComp.IsWorking = state;
        ent.Comp.IsConnected = state;
        ChangeConnectionLightVisualizer(ent);
        UpdateConnectionLights(ent);

        _popup.PopupPredicted(Loc.GetString(state ? "comp-genericfield-connected" : "comp-genericfield-disconnected"), ent, user);
        _audio.PlayPredicted(state ? ent.Comp.ActivationSound : ent.Comp.DeactivationSound, ent, user);
        if (HasComp<DeviceLinkSourceComponent>(ent))
        {
            _signal.SendSignal(ent, ent.Comp.ConnectionStatusPort, state);
            _signal.InvokePort(ent, state ? ent.Comp.FieldConnectedPort : ent.Comp.FieldDisconnectedPort);
        }
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Checks to see if this or the other gens connected to a new grid. If they did, remove connection.
    /// </summary>
    public void GridCheck(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (ent.Comp.ConnectedGenerator is not { } pair)
            return;

        var xFormQuery = GetEntityQuery<TransformComponent>();

        var gen1ParentGrid = xFormQuery.GetComponent(ent).GridUid;
        var gent2ParentGrid = xFormQuery.GetComponent(pair).GridUid;

        if (gen1ParentGrid != gent2ParentGrid)
            RemoveConnections(ent);
    }

    #endregion

    // Entered: coal mines
    #region Visualizer Helpers

    /// <summary>
    /// Creates a light component for the spawned fields.
    /// </summary>
    public void UpdateConnectionLights(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (_light.TryGetLight(ent, out var pointLightComponent))
            _light.SetEnabled(ent, ent.Comp.IsConnected, pointLightComponent);
    }

    /// <summary>
    /// Check if a fields power falls between certain ranges to update the field gen visual for power.
    /// </summary>
    private void ChangePowerVisualizer(Entity<GenericFieldGeneratorComponent> ent)
    {
        if (!TryComp<BatteryComponent>(ent, out var comp))
            return;
        var charge = comp.LastCharge;
        var maxCharge = comp.MaxCharge;

        _appearance.SetData(ent, GenericFieldGeneratorVisuals.PowerLight, (charge / maxCharge) switch
        {
            >= 0.99f => PowerLevelVisuals.FullPower,
            >= 0.80f => PowerLevelVisuals.VeryHighPower,
            >= 0.60f => PowerLevelVisuals.HighPower,
            >= 0.40f => PowerLevelVisuals.MediumPower,
            >= 0.20f => PowerLevelVisuals.LowPower,
            >= 0.01f => PowerLevelVisuals.MinimalPower,
            _ => PowerLevelVisuals.NoPower
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
