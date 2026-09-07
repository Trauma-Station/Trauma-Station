// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Nuclear.Reactor;

public abstract partial class SharedNuclearReactorSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] protected ISharedAdminLogManager AdminLog = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] private SharedDeviceLinkSystem _device = default!;
    [Dependency] private SharedNuclearMachineSystem _machine = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] protected EntityQuery<NuclearPropertiesComponent> PropsQuery = default!;
    [Dependency] protected EntityQuery<ReactorPartComponent> PartQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearReactorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NuclearReactorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<NuclearReactorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        Subs.BuiEvents<NuclearReactorComponent>(NuclearReactorUiKey.Key, subs =>
        {
            subs.Event<ReactorSwapPartMessage>(OnSwapPart);
            subs.Event<ReactorAdjustControlRodsMessage>(OnAdjustControlRods);
            subs.Event<ReactorEjectItemMessage>(OnEjectItem);
        });
    }

    private void OnInit(Entity<NuclearReactorComponent> ent, ref ComponentInit args)
    {
        _device.EnsureSinkPorts(ent.Owner, ent.Comp.ControlRodInsertionPort);
        _device.EnsureSourcePorts(ent.Owner, ent.Comp.ControlRodsAvgPort, ent.Comp.CasingTempPort);

        ent.Comp.PartsContainer = Container.EnsureContainer<Container>(ent.Owner, ent.Comp.PartsContainerName);

        if (_slots.TryGetSlot(ent.Owner, ent.Comp.PartSlotId, out var slot))
            ent.Comp.PartSlot = slot;
    }

    private void OnSwapPart(Entity<NuclearReactorComponent> ent, ref ReactorSwapPartMessage args)
    {
        var comp = ent.Comp;
        var pos = args.Position;
        if (pos.X < 0 || pos.Y < 0 || pos.X >= ent.Comp.GridWidth || pos.Y >= ent.Comp.GridHeight)
            return; // malf

        var part = GetEntity(comp.GetPart(pos.X, pos.Y));
        if ((comp.PartSlot.Item == null) == (part == null))
            return; // both empty or both full, cant work

        var user = args.Actor;
        if (comp.PartSlot.Item is not { } item)
        {
            if (!PartQuery.TryComp(part, out var partComp))
            {
                Log.Error($"Tried to swap invalid part {ToPrettyString(part)} in {ToPrettyString(ent)} at {pos}");
                return; // uh oh
            }

            if (partComp.Melted) // No removing a part if it's melted
            {
                Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg"), ent.Owner, args.Actor);
                return;
            }

            if (!Container.Insert(part.Value, ent.Comp.PartSlot.ContainerSlot!))
                return;

            partComp.Position = null;
            DirtyField(part.Value, partComp, nameof(ReactorPartComponent.Position));
            comp.SetPart(pos.X, pos.Y, null);
            AdminLog.Add(LogType.Action, $"{user:player} removed {part.Value:part} from position {pos.X},{pos.Y} in {ent.Owner:target}");
        }
        else
        {
            if (!PartQuery.TryComp(item, out var partComp))
            {
                Log.Error($"Tried ti swap invalid part {ToPrettyString(item)} in {ToPrettyString(ent)}'s part slot!");
                return;
            }

            if (!Container.Insert(item, ent.Comp.PartsContainer))
                return;

            comp.SetPart(pos.X, pos.Y, GetNetEntity(item));

            partComp.Position = pos;
            DirtyField(item, partComp, nameof(ReactorPartComponent.Position));

            AdminLog.Add(LogType.Action, $"{user:player} added {item:part} to position {pos.X},{pos.Y} in {ent.Owner:target}");
        }
        DirtyField(ent, ent.Comp, nameof(NuclearReactorComponent.PartGrid));

        UpdateGasVolume(ent);
        UpdateUI(ent);
    }

    private void OnEjectItem(Entity<NuclearReactorComponent> ent, ref ReactorEjectItemMessage args)
    {
        _slots.TryEjectToHands(ent.Owner, ent.Comp.PartSlot, args.Actor);
    }

    private void OnAdjustControlRods(Entity<NuclearReactorComponent> ent, ref ReactorAdjustControlRodsMessage args)
    {
        if (!AdjustControlRods(ent, args.Change))
            return;

        _machine.QueueLog(ent, args.Actor, args.Monitor);
        UpdateUI(ent);
    }

    private void OnSignalReceived(Entity<NuclearReactorComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.ControlRodInsertionPort)
            return; // wrong port

        int percent = 0;
        if (args.Data?.TryGetValue("logic_int", out percent) != true)
            return; // non circuit signal dont care

        SetTargetInsertion(ent, (float) percent * 0.01f);
    }

    private void OnUnanchorAttempt(EntityUid uid, NuclearReactorComponent comp, ref UnanchorAttemptEvent args)
    {
        var user = args.User;
        // One does not simply move a reactor that has welded itself in place
        if (comp.Melted)
        {
            Popup.PopupEntity(Loc.GetString("reactor-unanchor-melted"), user, user, PopupType.LargeCaution);
            args.Cancel();
            return;
        }

        if (comp.Temperature >= Atmospherics.T0C + 80 || !CheckEmpty(comp))
        {
            Popup.PopupEntity(Loc.GetString("reactor-unanchor-warning"), user, user, PopupType.LargeCaution);
            args.Cancel();
        }
    }

    private bool CheckEmpty(NuclearReactorComponent comp)
    {
        foreach (var part in comp.PartGrid)
        {
            if (part != null)
                return false;
        }

        return true;
    }

    public IEnumerable<(int, Entity<ReactorPartComponent>)> EnumerateParts(NuclearReactorComponent comp)
    {
        for (var i = 0; i < comp.PartGrid.Length; i++)
        {
            if (GetEntity(comp.PartGrid[i]) is not { } part)
                continue;

            if (PartQuery.TryComp(part, out var partComp))
                yield return (i, (part, partComp));
        }
    }

    public void UpdateUI(Entity<NuclearReactorComponent> ent)
    {
        // client doesnt have the data to predict the detailed slot state
        if (_net.IsClient || !_ui.IsUiOpen(ent.Owner, NuclearReactorUiKey.Key))
            return;

        if (ent.Comp.Melted)
        {
            _ui.CloseUi(ent.Owner, NuclearReactorUiKey.Key);
            return;
        }

        var size = ent.Comp.GridSize;
        var array = new ReactorSlotBUIData[size];
        for (var i = 0; i < size; i++)
        {
            var neutrons = ent.Comp.FluxGrid[i].Count;
            if (GetEntity(ent.Comp.PartGrid[i]) is not { } part)
            {
                array[i] = new ReactorSlotBUIData
                {
                    NeutronCount = neutrons
                };
                continue;
            }

            if (!PartQuery.TryComp(part, out var partComp) || !PropsQuery.TryComp(part, out var props))
            {
                Log.Error($"Found bad part {ToPrettyString(part)} in reactor {ToPrettyString(ent)} in slot {i}");
                continue;
            }

            array[i] = new ReactorSlotBUIData
            {
                Temperature = partComp.Temperature,
                NeutronCount = neutrons,
                NeutronRadioactivity = props.NeutronRadioactivity,
                Radioactivity = props.Radioactivity,
                SpentFuel = props.SpentFuel
            };
        }

        _ui.SetUiState(ent.Owner, NuclearReactorUiKey.Key, new NuclearReactorBuiState(array));
    }

    protected void UpdateVisuals(Entity<NuclearReactorComponent> ent)
    {
        var (uid, comp) = ent;
        if (comp.Melted)
        {
            Appearance.SetData(uid, ReactorVisuals.Lights, ReactorWarningLights.LightsOff);
            Appearance.SetData(uid, ReactorVisuals.Status, ReactorStatusLights.Off);
            Appearance.SetData(uid, ReactorVisuals.Input, false);
            Appearance.SetData(uid, ReactorVisuals.Output, false);
            return;
        }

        // Temperature & radiation warning
        var lights = ReactorWarningLights.LightsWarning;
        if (comp.Temperature < comp.ReactorOverheatTemp && comp.RadiationLevel < comp.MaximumRadiation * 0.5)
            lights = ReactorWarningLights.LightsOff;
        else if (comp.Temperature >= comp.ReactorFireTemp || comp.RadiationLevel > comp.MaximumRadiation)
            lights = ReactorWarningLights.LightsMeltdown;
        Appearance.SetData(uid, ReactorVisuals.Lights, lights);

        // Status screen / side lights
        Appearance.SetData(uid, ReactorVisuals.Status, comp.Temperature switch
        {
            var t when t > comp.ReactorFireTemp => ReactorStatusLights.Meltdown,
            var t when t > comp.ReactorOverheatTemp => ReactorStatusLights.Overheat,
            > Atmospherics.T20C => ReactorStatusLights.Active,
            _ => ReactorStatusLights.Off
        });
    }

    protected void UpdateTempIndicators(Entity<NuclearReactorComponent> ent)
    {
        var temp = ent.Comp.Temperature;
        SetSmoking(ent, temp >= ent.Comp.ReactorOverheatTemp);
        SetBurning(ent, temp >= ent.Comp.ReactorFireTemp);
    }

    protected virtual void UpdateGasVolume(Entity<NuclearReactorComponent> ent)
    {
        // nodegroup is server only
    }

    public bool AdjustControlRods(Entity<NuclearReactorComponent> ent, float change)
        => SetTargetInsertion(ent, ent.Comp.ControlRodInsertion + change);

    public bool SetTargetInsertion(Entity<NuclearReactorComponent> ent, float value)
    {
        value = Math.Clamp(value, 0, 2);
        if (ent.Comp.ControlRodInsertion == value)
            return false;

        ent.Comp.ControlRodInsertion = value;
        DirtyField(ent, ent.Comp, nameof(NuclearReactorComponent.ControlRodInsertion));
        return true;
    }

    public void SetAvgInsertion(Entity<NuclearReactorComponent> ent, float value)
    {
        // only need % precision for networking or circuits since thats the resolution
        var percent = (int) (value * 100);
        if ((int) (ent.Comp.AvgInsertion * 100) == percent)
            return;

        ent.Comp.AvgInsertion = value;
        DirtyField(ent, ent.Comp, nameof(NuclearReactorComponent.AvgInsertion));

        var payload = new NetworkPayload();
        payload["logic_int"] = percent;
        _device.InvokePort(ent.Owner, ent.Comp.ControlRodsAvgPort, payload);
    }

    public void SetTemperature(Entity<NuclearReactorComponent> ent, float temp)
    {
        if (ent.Comp.Temperature == temp)
            return;

        ent.Comp.Temperature = temp;
        DirtyField(ent, ent.Comp, nameof(NuclearReactorComponent.Temperature));

        var floored = (int) Math.Floor(temp);
        if (ent.Comp.LastSentTemp == floored)
            return;

        ent.Comp.LastSentTemp = floored;
        var payload = new NetworkPayload();
        payload["logic_int"] = floored;
        _device.InvokePort(ent, ent.Comp.CasingTempPort, payload);
    }

    public void SetThermalPower(Entity<NuclearReactorComponent> ent, int power)
    {
        if (ent.Comp.ThermalPower == power)
            return;

        ent.Comp.ThermalPower = power;
        DirtyField(ent, ent.Comp, nameof(NuclearReactorComponent.ThermalPower));

        var payload = new NetworkPayload();
        payload["logic_int"] = power;
        _device.InvokePort(ent, ent.Comp.ThermalPowerPort, payload);
    }

    public void SetSmoking(Entity<NuclearReactorComponent> ent, bool smoking)
    {
        if (ent.Comp.IsSmoking == smoking)
            return;

        ent.Comp.IsSmoking = smoking;
        DirtyField(ent, ent.Comp, nameof(NuclearReactorComponent.IsSmoking));

        Appearance.SetData(ent.Owner, ReactorVisuals.Smoke, smoking);
        var (word, popupType) = smoking
            ? ("start", PopupType.MediumCaution)
            : ("stop", PopupType.Medium);
        Popup.PopupEntity(Loc.GetString($"reactor-smoke-{word}", ("owner", ent)), ent, popupType);
        // TODO: smoke particle effects emitter
    }

    public void SetBurning(Entity<NuclearReactorComponent> ent, bool burning)
    {
        if (ent.Comp.IsBurning == burning)
            return;

        ent.Comp.IsBurning = burning;
        DirtyField(ent, ent.Comp, nameof(NuclearReactorComponent.IsBurning));

        Appearance.SetData(ent.Owner, ReactorVisuals.Fire, burning);
        var (word, popupType) = burning
            ? ("start", PopupType.LargeCaution)
            : ("stop", PopupType.Medium);
        Popup.PopupEntity(Loc.GetString($"reactor-fire-{word}", ("owner", ent)), ent, popupType);
        // TODO: fire particle effects emitter
    }
}
