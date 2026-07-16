// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.AlertLevel;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Content.Shared.Radio;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Nuclear;
using Content.Trauma.Shared.Nuclear.Reactor;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Nuclear.Reactor;

/// <summary>
/// Handles all nuclear reactor processing.
/// </summary>
/// <remarks>
/// Logic inspired by https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/nuclearreactor.dm
/// </remarks>
public sealed partial class NuclearReactorSystem : SharedNuclearReactorSystem
{
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private NuclearMachineSystem _machine = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private ReactorPartSystem _part = default!;
    [Dependency] private ServerGlobalSoundSystem _globalSound = default!;
    [Dependency] private SharedRadiationSystem _radiation = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private EntityQuery<ReactorControlRodComponent> _controlQuery = default!;
    [Dependency] private EntityQuery<ReactorGasChannelComponent> _channelQuery = default!;

    private List<Entity<ReactorPartComponent>> _neighbors = new(4);
    private List<ValueList<ReactorNeutron>> _flux = new(7*7);

    private static readonly EntProtoId NuclearDebrisChunk = "NuclearDebrisChunk";
    private static readonly ProtoId<WeightedRandomPrototype> NuclearReactorRandomParts = "NuclearReactorRandomParts";

    private static readonly TimeSpan _logDelay = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearReactorComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<NuclearReactorComponent, DamageDealtEvent>(OnDamageDealt);

        // Atmos events
        SubscribeLocalEvent<NuclearReactorComponent, AtmosDeviceUpdateEvent>(OnUpdate);

        SubscribeLocalEvent<NuclearReactorComponent, NuclearMachineLogEvent>(OnMachineLog);
    }

    private void OnMapInit(Entity<NuclearReactorComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;

        comp.PartGrid = new NetEntity?[comp.GridSize];
        comp.FluxGrid = new ValueList<ReactorNeutron>[comp.GridSize];

        ApplyPrefab(ent);

        // TODO: fix this shit
        // I hate everything about this, but it ensures the audio doesn't just stop if you don't look at it
        comp.AlarmAudioHighThermal = SpawnAttachedTo("ReactorAlarmEntity", new(uid, 0, 0));
        comp.AlarmAudioHighTemp = SpawnAttachedTo("ReactorAlarmEntity", new(uid, 0, 0));
        _ambient.SetSound(comp.AlarmAudioHighTemp.Value, new SoundPathSpecifier("/Audio/_FarHorizons/Machines/reactor_alarm_2.ogg"));
        comp.AlarmAudioHighRads = SpawnAttachedTo("ReactorAlarmEntity", new(uid, 0, 0));
        _ambient.SetSound(comp.AlarmAudioHighRads.Value, new SoundPathSpecifier("/Audio/_FarHorizons/Machines/reactor_alarm_3.ogg"));
    }

    #region Prefab
    private void ApplyPrefab(Entity<NuclearReactorComponent> ent)
    {
        var prefab = ent.Comp.Prefab is { } id ? ProtoMan.Index(id).Parts : GenerateRandomPrefab(ent.Comp);
        var container = ent.Comp.PartsContainerName;
        foreach (var (pos, partId) in prefab)
        {
            if (!TrySpawnInContainer(partId, ent, container, out var part))
                continue;

            var partComp = PartQuery.Comp(part.Value);
            partComp.Position = pos;
            DirtyField(part.Value, partComp, nameof(ReactorPartComponent.Position));
            ent.Comp.SetPart(pos.X, pos.Y, GetNetEntity(part));
        }
        DirtyField(ent, ent.Comp, nameof(NuclearReactorComponent.PartGrid));

        UpdateGasVolume(ent);
    }

    private Dictionary<Vector2i, EntProtoId> GenerateRandomPrefab(NuclearReactorComponent comp)
    {
        var parts = new Dictionary<Vector2i, EntProtoId>();
        var pool = ProtoMan.Index(NuclearReactorRandomParts);
        for (var x = 0; x < comp.GridWidth; x++)
        {
            for (var y = 0; y < comp.GridHeight; y++)
            {
                if (_random.Prob(comp.RandomPrefabFill))
                    parts[new(x, y)] = pool.Pick(_random);
            }
        }
        return parts;
    }
    #endregion

    #region Main Loop
    private void OnUpdate(Entity<NuclearReactorComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        var (uid, comp) = ent;
        ProcessCaseRadiation(ent);

        if (comp.Melted)
            return;

        if (!_machine.GetPipes(uid, out var inlet, out var outlet))
            return;

        var gridWidth = comp.GridWidth;
        var gridHeight = comp.GridHeight;

        Appearance.SetData(uid, ReactorVisuals.Input, inlet.Air.TotalMoles > 20);
        Appearance.SetData(uid, ReactorVisuals.Output, outlet.Air.TotalMoles > 20);

        var tempRads = 0;
        var energyChange = 0f;

        var transferVolume = CalculateTransferVolume(inlet.Air.Volume, inlet, outlet, args.dt);
        var gasInput = inlet.Air.RemoveVolume(transferVolume);

        gasInput.Volume = inlet.Volume;

        // Even though it's probably bad for performance, we have to do the for x, for y loops 3 times
        // to ensure the processes do not interfere with each other (TODO: no you dont...)

        // Rod interactions
        var avgControlRodInsertion = 0f;
        var controlRods = 0;
        foreach (var (i, part) in EnumerateParts(comp))
        {
            var props = PropsQuery.Comp(part);
            if (_channelQuery.TryComp(part, out var channel))
            {
                var gas = _part.ProcessGas((part, part.Comp, props, channel), ent, gasInput);
                gasInput.Volume -= channel.GasVolume;
                if (gas != null)
                    _atmos.Merge(outlet.Air, gas);
            }

            var pos = part.Comp.Position!.Value;
            GetGridNeighbors(comp, pos.X, pos.Y);
            _part.ProcessHeat(part, ent, _neighbors);

            comp.FluxGrid[i] = _part.ProcessNeutrons((part, part.Comp, props), comp.FluxGrid[i], out var deltaE);
            energyChange += deltaE;

            if (_controlQuery.TryComp(part, out var control))
            {
                // sync control rods target with the reactor
                if (control.ConfiguredInsertionLevel != comp.ControlRodInsertion)
                {
                    control.ConfiguredInsertionLevel = comp.ControlRodInsertion;
                    Dirty(part, control);
                }
                // fuel rod cross section is inversely proportional to control rod insertion
                avgControlRodInsertion += 1f - part.Comp.NeutronCrossSection;
                controlRods++;
            }
        }
        if (controlRods > 0)
            avgControlRodInsertion /= controlRods;
        SetAvgInsertion(ent, avgControlRodInsertion);

        // Sound for the control rods moving, basically an audio cue that the reactor's doing something important
        if (controlRods > 0 && !MathHelper.CloseTo(comp.AvgInsertion, avgControlRodInsertion))
            Audio.PlayPvs(new SoundPathSpecifier("/Audio/_FarHorizons/Machines/relay_click.ogg"), uid);

        // Snapshot of the flux grid that won't get messed up by the neutron calculations
        var size = comp.GridSize;
        _flux.Clear();
        foreach (var flux in comp.FluxGrid)
        {
            _flux.Add(new(flux));
        }

        // Move neutrons
        for (var y = 0; y < gridHeight; y++)
        {
            var row = y * gridWidth;
            for (var x = 0; x < gridWidth; x++)
            {
                var index = row + x;
                foreach (var neutron in _flux[index])
                {
                    var dir = (byte)neutron.Dir.AsFlag();
                    // Bit abuse
                    var xmod = ((dir >> 1) & 1) - ((dir >> 3) & 1);
                    var ymod = ((dir >> 2) & 1) - (dir & 1);

                    if (x + xmod >= 0 && y + ymod >= 0 && x + xmod <= gridWidth - 1 && y + ymod <= gridHeight - 1)
                        comp.GetFlux(x + xmod, y + ymod).Add(neutron);
                    else
                        tempRads++; // neutrons hitting the casing become radiation, too much and it will bypass shielding
                    comp.FluxGrid[index].Remove(neutron);
                }
            }
        }

        if (ProcessCasingGas(ent, gasInput) is { } casingGas)
            _atmos.Merge(outlet.Air, casingGas);

        // If there's still input gas left over
        _atmos.Merge(outlet.Air, gasInput);

        comp.RadiationLevel = Math.Max(comp.RadiationLevel + tempRads, 0);
        DirtyField(uid, comp, nameof(NuclearReactorComponent.RadiationLevel));

        // W = J/s
        // use a rolling average to not jump erratically
        var currentPower = energyChange / args.dt;
        if (comp.ThermalPowerCount < comp.ThermalPowerPrecision)
            comp.ThermalPowerCount++;
        SetThermalPower(ent, comp.ThermalPower + (int) ((currentPower - comp.ThermalPower) / Math.Min(comp.ThermalPowerCount, comp.ThermalPowerPrecision)));

        if (comp.Temperature > comp.ReactorMeltdownTemp)
        {
            CatastrophicOverload(ent);
        }

        UpdateVisuals(ent);
        UpdateAudio(ent);
        UpdateRadio(ent);
        UpdateTempIndicators(ent);

        UpdateUI(ent);
    }

    private void ProcessCaseRadiation(Entity<NuclearReactorComponent> ent)
    {
        var (uid, comp) = ent;
        var source = EnsureComp<RadiationSourceComponent>(uid);

        // shielding protects up to MaximumRadiation, linear scaling past that
        _radiation.SetIntensity((uid, source), MathF.Max(
            comp.RadiationLevel - comp.MaximumRadiation,
            comp.Melted ? comp.MeltdownRadiation : 0));
        comp.RadiationLevel /= comp.RadiationStability;
        DirtyField(uid, comp, nameof(NuclearReactorComponent.RadiationLevel));
    }

    private void GetGridNeighbors(NuclearReactorComponent comp, int x, int y)
    {
        _neighbors.Clear();
        if (x - 1 >= 0)
            TryAddNeighbor(comp, x - 1, y);
        if (x + 1 < comp.GridWidth)
            TryAddNeighbor(comp, x + 1, y);
        if (y - 1 >= 0)
            TryAddNeighbor(comp, x, y - 1);
        if (y + 1 < comp.GridHeight)
            TryAddNeighbor(comp, x, y + 1);
    }

    private void TryAddNeighbor(NuclearReactorComponent comp, int x, int y)
    {
        if (GetPart(comp, x, y) is { } part)
            _neighbors.Add(part);
    }

    private Entity<ReactorPartComponent>? GetPart(NuclearReactorComponent reactor, int x, int y)
        => GetEntity(reactor.GetPart(x, y)) is { } uid && PartQuery.TryComp(uid, out var comp)
            ? (uid, comp)
            : null;

    protected override void UpdateGasVolume(Entity<NuclearReactorComponent> ent)
    {
        if (!_machine.GetPipes(ent.Owner, out var inlet, out var outlet))
            return;

        var totalGasVolume = ent.Comp.ReactorVesselGasVolume;

        foreach (var (_, part) in EnumerateParts(ent.Comp))
        {
            if (_channelQuery.TryComp(part, out var channel))
                totalGasVolume += channel.GasVolume;
        }

        inlet.Volume = totalGasVolume;
        outlet.Volume = totalGasVolume;
    }

    private GasMixture? ProcessCasingGas(Entity<NuclearReactorComponent> ent, GasMixture inGas)
    {
        var (uid, comp) = ent;
        var props = PropsQuery.Comp(uid);
        GasMixture? processedGas = null;
        if (comp.AirContents != null)
        {
            var deltaT = comp.Temperature - comp.AirContents.Temperature;
            var deltaTr = Math.Pow(comp.Temperature, 4) - Math.Pow(comp.AirContents.Temperature, 4);

            var k = props.CalculateHeatTransferCoefficient();
            var a = 1 * (0.4 * 8);

            var thermalEnergy = _atmos.GetThermalEnergy(comp.AirContents);

            var hottest = Math.Max(comp.AirContents.Temperature, comp.Temperature);
            var coldest = Math.Min(comp.AirContents.Temperature, comp.Temperature);

            var energy = comp.Temperature * comp.ThermalMass;
            // project 0 documentation
            var maxDeltaE = Math.Clamp((k * a * deltaT) + (5.67037442e-8 * a * deltaTr),
                energy - (hottest * comp.ThermalMass),
                energy - (coldest * comp.ThermalMass));

            comp.AirContents.Temperature = (float)Math.Clamp(comp.AirContents.Temperature +
                (maxDeltaE / _atmos.GetHeatCapacity(comp.AirContents, true)), coldest, hottest);

            SetTemperature(ent, (float)Math.Clamp(comp.Temperature -
                ((_atmos.GetThermalEnergy(comp.AirContents) - thermalEnergy) / comp.ThermalMass), coldest, hottest));

            if (comp.AirContents.Temperature < 0 || comp.Temperature < 0)
                throw new Exception("Reactor casing temperature calculation resulted in sub-zero value.");

            processedGas = comp.AirContents;
        }

        if (inGas != null && _atmos.GetThermalEnergy(inGas) > 0)
        {
            comp.AirContents = inGas.RemoveVolume(comp.ReactorVesselGasVolume);

            if (comp.AirContents != null && comp.AirContents.TotalMoles < 1)
            {
                if (processedGas != null)
                    _atmos.Merge(processedGas, comp.AirContents);
                else
                    processedGas = comp.AirContents;
                comp.AirContents.Clear();
            }
        }

        return processedGas;
    }

    private float CalculateTransferVolume(float volume, PipeNode inlet, PipeNode outlet, float dt)
    {
        var wantToTransfer = volume * _atmos.PumpSpeedup() * dt;
        var transferVolume = Math.Min(inlet.Air.Volume, wantToTransfer);
        var transferMoles = inlet.Air.Pressure * transferVolume / (inlet.Air.Temperature * Atmospherics.R);
        var molesSpaceLeft = ((Atmospherics.MaxOutputPressure * 3) - outlet.Air.Pressure) * outlet.Air.Volume / (outlet.Air.Temperature * Atmospherics.R);
        var actualMolesTransfered = Math.Clamp(transferMoles, 0, Math.Max(0, molesSpaceLeft));
        return Math.Max(0, actualMolesTransfered * inlet.Air.Temperature * Atmospherics.R / inlet.Air.Pressure);
    }

    private void CatastrophicOverload(Entity<NuclearReactorComponent> ent)
    {
        var (uid, comp) = ent;
        if (_station.GetStationInMap(Transform(uid).MapID) is { } station)
            _alertLevel.SetLevel(station, comp.MeltdownAlertLevel, true, true, true);

        var announcement = Loc.GetString("reactor-meltdown-announcement");
        var sender = Loc.GetString("reactor-meltdown-announcement-sender");
        _chat.DispatchStationAnnouncement(uid, announcement, sender, false, null, Color.Orange);

        _globalSound.PlayGlobalOnStation(uid, Audio.ResolveSound(comp.MeltdownSound));

        comp.Melted = true;
        Appearance.SetData(uid, ReactorVisuals.Melted, true);
        var badness = 0f;
        comp.AirContents ??= new();

        foreach (var (_, part) in EnumerateParts(comp))
        {
            if (!PropsQuery.TryComp(part, out var props))
                continue;

            // TODO: make this an event?
            var bad = (props.Radioactivity * 2) + (props.NeutronRadioactivity * 5) + (props.SpentFuel * 10);
            if (part.Comp.Melted)
                bad *= 2;
            badness += bad;
            // TODO: move this to event for gas channel comp
            if (_channelQuery.TryComp(part, out var channel) && channel.AirContents is { } partAir)
                _atmos.Merge(comp.AirContents, partAir);
        }
        comp.RadiationLevel = Math.Clamp(comp.RadiationLevel + badness, 0, 200);
        comp.AirContents.AdjustMoles(Gas.Tritium, badness * 15);
        comp.AirContents.Temperature = Math.Max(comp.Temperature, comp.AirContents.Temperature);

        var T = _atmos.GetTileMixture(ent.Owner, excite: true);
        if (T != null)
            _atmos.Merge(T, comp.AirContents);

        AdminLog.Add(LogType.Explosion, LogImpact.Extreme, $"{uid:reactor} melted down with badness of {badness}!");

        // You did not see graphite on the roof. You're in shock. Report to medical.
        var coords = _transform.GetMapCoordinates(uid);
        for (var i = 0; i < _random.Next(10, 30); i++)
        {
            _throwing.TryThrow(Spawn(NuclearDebrisChunk, coords), _random.NextAngle().ToVec().Normalized(), _random.NextFloat(8, 16),
                uid, predicted: false);
        }

        Audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/metal_break5.ogg"), uid);
        _explosion.QueueExplosion(ent.Owner, "Radioactive", Math.Max(100, badness * 5), 1, 5, 0, canCreateVacuum: false);

        var lightcomp = _light.EnsureLight(uid);
        _light.SetEnergy(uid, 0.1f, lightcomp);
        _light.SetFalloff(uid, 2, lightcomp);
        _light.SetRadius(uid, (comp.GridWidth + comp.GridHeight) / 4, lightcomp);
        _light.SetColor(uid, Color.FromHex("#FFAAAAFF"), lightcomp);

        // Reset grids
        Container.CleanContainer(comp.PartsContainer);
        for (var i = 0; i < comp.GridSize; i++)
        {
            comp.PartGrid[i] = null;
            comp.FluxGrid[i].Clear();
        }
        DirtyField(uid, comp, nameof(NuclearReactorComponent.PartGrid));
    }

    private void UpdateAudio(Entity<NuclearReactorComponent> ent)
    {
        var comp = ent.Comp;

        if (Exists(comp.AlarmAudioHighThermal))
            _ambient.SetAmbience(comp.AlarmAudioHighThermal.Value, !comp.Melted && comp.ThermalPower > comp.MaximumThermalPower);
        if (Exists(comp.AlarmAudioHighTemp))
            _ambient.SetAmbience(comp.AlarmAudioHighTemp.Value, !comp.Melted && comp.Temperature > comp.ReactorOverheatTemp);
        if (Exists(comp.AlarmAudioHighRads))
            _ambient.SetAmbience(comp.AlarmAudioHighRads.Value, !comp.Melted && comp.RadiationLevel > comp.MaximumRadiation * 0.5);
    }

    private void UpdateRadio(Entity<NuclearReactorComponent> ent)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;

        if (comp.Melted)
            return;

        var engi = ProtoMan.Index(ent.Comp.AlertsChannel);
        if (comp.Temperature >= comp.ReactorOverheatTemp)
        {
            if (!comp.IsSmoking)
            {
                AdminLog.Add(LogType.Damaged, $"{ent.Owner:reactor} is at {comp.Temperature}K and may meltdown");
                _radio.SendRadioMessage(uid, Loc.GetString("reactor-smoke-start-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent);
                comp.LastSendTemperature = comp.Temperature;
            }
            if (comp.Temperature >= comp.ReactorFireTemp && !comp.IsBurning)
            {
                AdminLog.Add(LogType.Damaged, $"{ent.Owner:reactor} is at {comp.Temperature}K and is likely to meltdown");
                _radio.SendRadioMessage(uid, Loc.GetString("reactor-fire-start-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent);
                comp.LastSendTemperature = comp.Temperature;
            }
            else if (comp.Temperature < comp.ReactorFireTemp && comp.IsBurning)
            {
                AdminLog.Add(LogType.Healed, $"{ent.Owner:reactor} is cooling from {comp.ReactorFireTemp}K");
                _radio.SendRadioMessage(uid, Loc.GetString("reactor-fire-stop-message", ("owner", uid)), engi, ent);
                comp.LastSendTemperature = comp.Temperature;
            }
        }
        else
        {
            if (comp.IsSmoking)
            {
                AdminLog.Add(LogType.Healed, $"{ent.Owner:reactor} is cooling from {comp.ReactorOverheatTemp}K");
                _radio.SendRadioMessage(uid, Loc.GetString("reactor-smoke-stop-message", ("owner", uid)), engi, ent);
                comp.LastSendTemperature = comp.Temperature;
                comp.HasSentWarning = false;
            }
        }

        if (comp.Temperature >= (comp.ReactorFireTemp + comp.ReactorMeltdownTemp) / 2 && !comp.HasSentWarning)
        {
            var stationUid = _station.GetStationInMap(Transform(uid).MapID);
            var announcement = Loc.GetString("reactor-melting-announcement");
            var sender = Loc.GetString("reactor-melting-announcement-sender");
            _chat.DispatchStationAnnouncement(stationUid ?? uid, announcement, sender, false, null, Color.Orange);
            _globalSound.PlayGlobalOnStation(uid, Audio.ResolveSound(new SoundPathSpecifier("/Audio/Misc/delta.ogg")));
            comp.HasSentWarning = true;
        }

        if (Math.Max(comp.LastSendTemperature, comp.Temperature) < comp.ReactorOverheatTemp)
            return;

        var step = comp.ReactorMeltdownTemp * 0.05;

        if (Math.Abs(comp.Temperature - comp.LastSendTemperature) < step)
            return;

        if (comp.LastSendTemperature > comp.Temperature)
        {
            _radio.SendRadioMessage(uid, Loc.GetString("reactor-temperature-cooling-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent);
        }
        else
        {
            if (comp.Temperature >= comp.ReactorFireTemp)
            {
                _radio.SendRadioMessage(uid, Loc.GetString("reactor-temperature-critical-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent);
            }
            else if (comp.Temperature >= comp.ReactorOverheatTemp)
            {
                _radio.SendRadioMessage(uid, Loc.GetString("reactor-temperature-dangerous-message", ("owner", uid), ("temperature", Math.Round(comp.Temperature))), engi, ent);
            }
        }

        comp.LastSendTemperature = comp.Temperature;
    }
    #endregion

    private void OnMachineLog(Entity<NuclearReactorComponent> ent, ref NuclearMachineLogEvent args)
    {
        AdminLog.Add(LogType.Action, $"{args.User:player} set control rod insertion of {ent.Owner:target} to {ent.Comp.ControlRodInsertion} using {args.Monitor:monitor}");
    }

    private void OnDamageDealt(Entity<NuclearReactorComponent> ent, ref DamageDealtEvent args)
    {
        var damage = (float) args.Damage.GetTotal();
        if (damage < 0f)
            return;

        var destruction = 100;
        var throwProb = Math.Clamp(damage / destruction, 0, 1);
        var coords = _transform.GetMapCoordinates(ent);
        var size = ent.Comp.GridSize;
        var dirty = false;
        for (var i = 0; i < size; i++)
        {
            if (!_random.Prob(throwProb))
                continue;

            if (GetEntity(ent.Comp.PartGrid[i]) is not { } part || !PartQuery.TryComp(part, out var partComp))
                continue;

            var item = part;
            if (_random.Prob(0.5f) || partComp.Melted)
            {
                QueueDel(part);
                item = Spawn(NuclearDebrisChunk, coords);
            }

            _throwing.TryThrow(item, _random.NextAngle().ToVec(), _random.NextFloat(8, 16), ent,
                predicted: false);
            var x = i % ent.Comp.GridWidth;
            var y = i / ent.Comp.GridWidth;
            AdminLog.Add(LogType.Action, $"Damage by {args.Origin:actor} removed {part:part} from position {x},{y} in {ent.Owner:reactor}");

            ent.Comp.PartGrid[i] = null;
            dirty = true;
        }

        if (!dirty)
            return;

        DirtyField(ent, ent.Comp, nameof(NuclearReactorComponent.PartGrid));
        UpdateGasVolume(ent);
    }
}
