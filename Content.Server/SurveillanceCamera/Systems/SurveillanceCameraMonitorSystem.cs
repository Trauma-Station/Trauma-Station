// <Trauma>
using Content.Goobstation.Common.SurveillanceCamera;
using Robust.Server.GameStates;
using Robust.Shared.Map;
using System.Runtime.InteropServices;
// </Trauma>
using System.Linq;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Content.Shared.SurveillanceCamera;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraMonitorSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;
    // </Trauma>
    [Dependency] private SurveillanceCameraSystem _surveillanceCameras = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private DeviceNetworkRouterSystem _deviceNetworkRouter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, SurveillanceCameraDeactivateEvent>(OnSurveillanceCameraDeactivate);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, ComponentShutdown>(OnShutdown);
        // SubscribeLocalEvent<SurveillanceCameraMonitorComponent, ComponentStartup>(OnComponentStartup); // Trauma
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        Subs.BuiEvents<SurveillanceCameraMonitorComponent>(SurveillanceCameraMonitorUiKey.Key, subs =>
        {
            subs.Event<SurveillanceCameraRefreshCamerasMessage>(OnRefreshCamerasMessage);
            subs.Event<SurveillanceCameraRefreshSubnetsMessage>(OnRefreshSubnetsMessage);
            subs.Event<SurveillanceCameraDisconnectMessage>(OnDisconnectMessage);
            //subs.Event<SurveillanceCameraMonitorSubnetRequestMessage>(OnSubnetRequest); // Trauma
            subs.Event<SurveillanceCameraMonitorSwitchMessage>(OnSwitchMessage);
            subs.Event<BoundUIClosedEvent>(OnBoundUiClose);
        });
    }

    private const float MaxHeartbeatTime = 3f; // Trauma - was 300
    private const float HeartbeatDelay = 1f; // Trauma - was 30

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveSurveillanceCameraMonitorComponent, SurveillanceCameraMonitorComponent>();
        while (query.MoveNext(out var uid, out _, out var monitor))
        {
            monitor.LastHeartbeatSent += frameTime;
            SendHeartbeat(uid, monitor.ActiveCameraAddress, monitor); // Trauma - added monitor.ActiveCameraAddress
            monitor.LastHeartbeat += frameTime;

            if (monitor.LastHeartbeat > MaxHeartbeatTime)
            {
                DisconnectCamera(uid, true, monitor);
                RemComp<ActiveSurveillanceCameraMonitorComponent>(uid);
                // <Trauma>
                monitor.LastHeartbeatSent = 0f;
                monitor.LastHeartbeat = 0f;
                RefreshCameras(uid, monitor);
                // </Trauma>
            }
        }
        // <Trauma>
        // TODO: move this shit out
        var query2 = EntityQueryEnumerator<HasMobileCamerasSurveillanceCameraMonitorComponent, SurveillanceCameraMonitorComponent>();
        while (query2.MoveNext(out var uid, out var active, out var monitor))
        {
            if (monitor.KnownMobileCameras.Count > 0)
            {
                // Collect expired cameras and cache their entity references
                var expiredCameras = new Dictionary<string, EntityUid>();

                foreach (var (key, cameraData) in monitor.KnownMobileCameras)
                {
                    ref var lastSent = ref CollectionsMarshal.GetValueRefOrAddDefault(
                        monitor.KnownMobileCamerasLastHeartbeatSent, key, out bool sentExists);
                    ref var lastHeartbeat = ref CollectionsMarshal.GetValueRefOrAddDefault(
                        monitor.KnownMobileCamerasLastHeartbeat, key, out bool hbExists);

                    if (!sentExists) lastSent = 0f;
                    if (!hbExists) lastHeartbeat = 0f;

                    lastSent += frameTime;
                    lastHeartbeat += frameTime;

                    SendHeartbeat(uid, key, monitor);

                    if (lastHeartbeat > MaxHeartbeatTime)
                        expiredCameras[key] = GetEntity(cameraData.Item2);
                }

                // Remove PVS overrides for all viewers in a single pass
                foreach (var player in monitor.Viewers)
                {
                    if (!TryComp<ActorComponent>(player, out var actor))
                        continue;

                    foreach (var entity in expiredCameras.Values)
                        _pvsOverride.RemoveSessionOverride(entity, actor.PlayerSession);
                }

                // Remove expired cameras from all dictionaries
                foreach (var key in expiredCameras.Keys)
                {
                    monitor.KnownMobileCameras.Remove(key);
                    monitor.KnownMobileCamerasLastHeartbeat.Remove(key);
                    monitor.KnownMobileCamerasLastHeartbeatSent.Remove(key);
                }

                // Cleanup component if empty
                if (monitor.KnownMobileCameras.Count == 0)
                    RemCompDeferred(uid, active);

                // Refresh subnets as clearly something went wrong with the networking
                if (expiredCameras.Count > 0)
                    RefreshCameras(uid, monitor);
            }
        }
        // </Trauma>
    }

    /// ROUTING:
    ///
    /// Monitor freq: General frequency for cameras, routers, and monitors to speak on.
    ///
    /// Subnet freqs: Frequency for each specific subnet. Routers ping cameras here,
    ///               cameras ping back on monitor frequency. When a monitor
    ///               selects a subnet, it saves that subnet's frequency
    ///               so it can connect to the camera. All outbound cameras
    ///               always speak on the monitor frequency and will not
    ///               do broadcast pings - whatever talks to it, talks to it.
    ///
    /// How a camera is discovered:
    ///
    /// Subnet ping:
    /// Surveillance camera monitor - [ monitor freq ] -> Router
    /// Router -> camera discovery
    /// Router - [ subnet freq ] -> Camera
    /// Camera -> router ping
    /// Camera - [ monitor freq ] -> Router
    /// Router -> monitor data forward
    /// Router - [ monitor freq ] -> Monitor

    #region Event Handling
    /* Trauma
    private void OnComponentStartup(EntityUid uid, SurveillanceCameraMonitorComponent component, ComponentStartup args)
    {
        RefreshSubnets(uid, component);
    }

    private void OnSubnetRequest(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraMonitorSubnetRequestMessage args)
    {
        if (args.Actor is { Valid: true } actor && !Deleted(actor))
        {
            SetActiveSubnet(uid, args.Subnet, component);
        }
    }
    */

    [SubscribeLocalEvent]
    private void OnCameraConnect(Entity<SurveillanceCameraMonitorComponent> ent, ref DeviceNetworkPacketEvent<SurveillanceCameraConnectPayload> args)
    {
        var payload = args.Data;
        if (ent.Comp.NextCameraAddress == payload.SenderAddress)
        {
            if (payload.SenderAddress != null)
                ent.Comp.ActiveCameraAddress = payload.SenderAddress;
            TrySwitchCameraByUid(ent, payload.Sender, ent.Comp);
        }

        ent.Comp.NextCameraAddress = null;
    }

    [SubscribeLocalEvent]
    private void OnCameraHeartbeat(Entity<SurveillanceCameraMonitorComponent> ent, ref DeviceNetworkPacketEvent<SurveillanceCameraHeartbeatPayload> args)
    {
        if (args.Data.SenderAddress == ent.Comp.ActiveCameraAddress)
        {
            // <Trauma> - replaces single heartbeat with per-sender dict
            if (ent.Comp.KnownMobileCamerasLastHeartbeat.ContainsKey(args.SenderAddress))
                ent.Comp.KnownMobileCamerasLastHeartbeat[args.SenderAddress] = 0;
            // </Trauma>
        }
    }

    [SubscribeLocalEvent]
    private void OnCameraData(Entity<SurveillanceCameraMonitorComponent> ent, ref DeviceNetworkPacketEvent<SurveillanceCameraDataPayload> args)
    {
        var payload = args.Data;
        /* <Trauma>
        var subnetData = payload.Subnet;

        if (ent.Comp.ActiveSubnet != subnetData)
        {
            DisconnectFromSubnet(ent, subnetData);
        }
        */
        if (payload.SenderAddress is not { } addr)
            return;

        var info = (payload.Name, GetNetEntity(payload.Sender), GetNetCoordinates(payload.Position));
        if (payload.IsMobile) // mobile cameras go in their own list
        {
            if (ent.Comp.KnownMobileCameras.Count == 0) // was it the first mobile camera added?
                EnsureComp<HasMobileCamerasSurveillanceCameraMonitorComponent>(ent);
            ent.Comp.KnownMobileCameras.TryAdd(addr, info);
        }
        else if (!ent.Comp.KnownCameras.ContainsKey(addr))
        {
            ent.Comp.KnownCameras.Add(addr, info); // replaced name with info tuple
        }
        // </Trauma>
        UpdateUserInterface(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnSubnetData(Entity<SurveillanceCameraMonitorComponent> ent, ref DeviceNetworkPacketEvent<SurveillanceCameraSubnetDataPayload> args)
    {
        ent.Comp.KnownSubnets.TryAdd(args.Data.TransmitFrequency, args.SenderAddress);
        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnDisconnectMessage(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraDisconnectMessage message)
    {
        DisconnectCamera(uid, true, component);
    }

    private void OnRefreshCamerasMessage(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraRefreshCamerasMessage message)
    {
        // <Trauma> - replaced clear + ping with this
        RefreshCameras(uid, component);
        // </Trauma>
    }

    // <Trauma>
    private void RefreshCameras(EntityUid uid, SurveillanceCameraMonitorComponent comp)
    {
        foreach (var player in comp.Viewers)
        {
            if (TryComp<ActorComponent>(player, out var actor))
            {
                foreach (var camera in comp.KnownMobileCameras.Values)
                {
                    _pvsOverride.RemoveSessionOverride(GetEntity(camera.Item2), actor.PlayerSession);
                }
            }
        }
        comp.KnownCameras.Clear();
        comp.KnownMobileCameras.Clear();
        PingSubnets(uid, comp);

        foreach (var (subnet, address) in comp.KnownSubnets)
        {
            var payload = new SurveillanceCameraSubnetConnectPayload();
            _deviceNetworkSystem.SendPacket(uid, address, ref payload);
        }
    }
    // </Trauma>

    private void OnRefreshSubnetsMessage(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraRefreshSubnetsMessage message)
    {
        RefreshSubnets(uid, component);
    }

    private void OnSwitchMessage(EntityUid uid, SurveillanceCameraMonitorComponent component, SurveillanceCameraMonitorSwitchMessage message)
    {
        // there would be a null check here, but honestly
        // whichever one is the "latest" switch message gets to
        // do the switch
        TrySwitchCameraByAddress(uid, message.Address, component);
    }

    private void OnPowerChanged(EntityUid uid, SurveillanceCameraMonitorComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            RemoveActiveCamera(uid, component);
            component.NextCameraAddress = null;
            // Goobstation start
            foreach (var subnetwork in component.KnownSubnets.Values)
                DisconnectFromSubnet(uid, subnetwork);
            // Goobstation end
        }
    }

    private void OnShutdown(EntityUid uid, SurveillanceCameraMonitorComponent component, ComponentShutdown args)
    {
        RemoveActiveCamera(uid, component);
    }

    private void OnToggleInterface(EntityUid uid, SurveillanceCameraMonitorComponent component,
        AfterActivatableUIOpenEvent args)
    {
        AfterOpenUserInterface(uid, args.User, component);
    }

    // This is to ensure that there's no delay in ensuring that a camera is deactivated.
    private void OnSurveillanceCameraDeactivate(EntityUid uid, SurveillanceCameraMonitorComponent monitor, SurveillanceCameraDeactivateEvent args)
    {
        DisconnectCamera(uid, false, monitor);
    }

    private void OnBoundUiClose(EntityUid uid, SurveillanceCameraMonitorComponent component, BoundUIClosedEvent args)
    {
        RemoveViewer(uid, args.Actor, component);
    }

    #endregion

    private void SendHeartbeat(EntityUid uid, string cameraAddress, SurveillanceCameraMonitorComponent? monitor = null) // Trauma - added cameraAddress
    {
        if (!Resolve(uid, ref monitor)
            || monitor.LastHeartbeatSent < HeartbeatDelay)
            /* Trauma
            || monitor.ActiveSubnet is not { } activeSubnet
            || !monitor.KnownSubnets.TryGetValue(activeSubnet, out var subnetAddress))
            */
        {
            return;
        }

        var payload = new SurveillanceCameraHeartbeatRequestPayload();
        // <Trauma> - send it to all routers instead of just the active one, use cameraAddress param
        foreach (var (subnet, subnetAddress) in monitor.KnownSubnets)
        {
            var freq = ProtoMan.Index(subnet).Frequency;
            _deviceNetworkRouter.SendPacketRouted(uid, ref payload, subnetAddress, cameraAddress, freq);
        }
        monitor.LastHeartbeatSent = 0;
        // </Trauma>
    }

    private void DisconnectCamera(EntityUid uid, bool removeViewers, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        if (removeViewers)
        {
            RemoveActiveCamera(uid, monitor);
        }

        monitor.ActiveCamera = null;
        monitor.ActiveCameraAddress = string.Empty;
        RemComp<ActiveSurveillanceCameraMonitorComponent>(uid);
        UpdateUserInterface(uid, monitor);
    }

    private void RefreshSubnets(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        // Goobstation start
        foreach (var subnetAddress in monitor.KnownSubnets.Values)
        {
            var payload = new SurveillanceCameraSubnetDisconnectPayload();
            _deviceNetworkSystem.SendPacket(uid, subnetAddress, ref payload);
        }
        // Goobstation end

        monitor.KnownSubnets.Clear();
        PingSubnets(uid, monitor);
    }

    /* Trauma
    private void PingCameraNetwork(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || monitor.ActiveSubnet == null
            || !monitor.KnownSubnets.TryGetValue(monitor.ActiveSubnet.Value, out var subnetData))
            return;

        var payload = new SurveillanceCameraPingPayload { Subnet = monitor.ActiveSubnet };
        _deviceNetworkRouter.SendPacketRouted(uid, ref payload, null, null, ProtoMan.Index(monitor.ActiveSubnet.Value).Frequency);
    }

    private void SetActiveSubnet(EntityUid uid, ProtoId<DeviceFrequencyPrototype> subnet,
        SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !monitor.KnownSubnets.ContainsKey(subnet))
        {
            return;
        }

        if (monitor.ActiveSubnet is { } previousSubnet)
            DisconnectFromSubnet(uid, previousSubnet);
        DisconnectCamera(uid, true, monitor);
        monitor.ActiveSubnet = subnet;
        monitor.KnownCameras.Clear();
        UpdateUserInterface(uid, monitor);

        ConnectToSubnet(uid, subnet);
    }
    */

    private void PingSubnets(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        var payload = new SurveillanceCameraPingSubnetPayload();
        _deviceNetworkSystem.SendPacket(uid, null, ref payload);
    }

    /* Trauma
    private void ConnectToSubnet(EntityUid uid, string subnet, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || string.IsNullOrEmpty(subnet)
            || !monitor.KnownSubnets.TryGetValue(subnet, out var address))
        {
            return;
        }

        var payload = new SurveillanceCameraSubnetConnectPayload();
        _deviceNetworkSystem.SendPacket(uid, address, ref payload);

        PingSubnets(uid);
    }
    */

    private void DisconnectFromSubnet(EntityUid uid, ProtoId<DeviceFrequencyPrototype> subnet, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !monitor.KnownSubnets.TryGetValue(subnet, out var address))
        {
            return;
        }

        var payload = new SurveillanceCameraSubnetDisconnectPayload();
        _deviceNetworkSystem.SendPacket(uid, address, ref payload);
    }

    // Adds a viewer to the camera and the monitor.
    private void AddViewer(EntityUid uid, EntityUid player, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        monitor.Viewers.Add(player);

        // Goobstation start
        if (TryComp<ActorComponent>(player, out var actor))
        {
            foreach (var camera in monitor.KnownMobileCameras.Values)
            {
                _pvsOverride.AddSessionOverride(GetEntity(camera.Item2), actor.PlayerSession);
            }
        }
        // Goobstation end

        if (monitor.ActiveCamera != null)
        {
            _surveillanceCameras.AddActiveViewer(monitor.ActiveCamera.Value, player, uid);
        }

        UpdateUserInterface(uid, monitor, player);
    }

    // Removes a viewer from the camera and the monitor.
    private void RemoveViewer(EntityUid uid, EntityUid player, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        monitor.Viewers.Remove(player);

        // Goobstation end
        if (TryComp<ActorComponent>(player, out var actor))
        {
            foreach (var camera in monitor.KnownMobileCameras.Values)
            {
                _pvsOverride.RemoveSessionOverride(GetEntity(camera.Item2), actor.PlayerSession);
            }
        }
        // Goobstation start

        if (monitor.ActiveCamera != null)
        {
            _surveillanceCameras.RemoveActiveViewer(monitor.ActiveCamera.Value, player);
        }
    }

    // Sets the camera. If the camera is not null, this will return.
    // The camera should always attempt to switch over, rather than
    // directly setting it, so that the active viewer list and view
    // subscriptions can be updated.
    private void SetCamera(EntityUid uid, EntityUid camera, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || monitor.ActiveCamera != null)
        {
            return;
        }

        _surveillanceCameras.AddActiveViewers(camera, monitor.Viewers, uid);

        monitor.ActiveCamera = camera;

        // Reset the heartbeat timers for the new device.
        monitor.LastHeartbeat = 0;
        monitor.LastHeartbeatSent = 0;

        AddComp<ActiveSurveillanceCameraMonitorComponent>(uid);

        UpdateUserInterface(uid, monitor);
    }

    // Switches the camera's viewers over to this new given camera.
    private void SwitchCamera(EntityUid uid, EntityUid camera, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || monitor.ActiveCamera == null)
        {
            return;
        }

        _surveillanceCameras.SwitchActiveViewers(monitor.ActiveCamera.Value, camera, monitor.Viewers, uid);

        monitor.ActiveCamera = camera;

        // Reset the heartbeat timers for the new device.
        monitor.LastHeartbeat = 0;
        monitor.LastHeartbeatSent = 0;

        UpdateUserInterface(uid, monitor);
    }

    private void TrySwitchCameraByAddress(EntityUid uid, string address,
        SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
            return;

        /* Trauma
        if (cameraSubnet != null && cameraSubnet != monitor.ActiveSubnet)
            SetActiveSubnet(uid, cameraSubnet, monitor);

        if (monitor.ActiveSubnet is not { } activeSubnet
            || !monitor.KnownSubnets.TryGetValue(activeSubnet, out var subnetAddress))
            return;
        */

        var payload = new SurveillanceCameraConnectRequestPayload();
        monitor.NextCameraAddress = address;
        // <Trauma> - send it to every router
        foreach (var (subnet, subnetAddress) in monitor.KnownSubnets)
        {
            var freq = ProtoMan.Index(subnet).Frequency;
            _deviceNetworkRouter.SendPacketRouted(uid, ref payload, subnetAddress, address, freq);
        }
        // </Trauma>
    }

    // Attempts to switch over the current viewed camera on this monitor
    // to the new camera.
    private void TrySwitchCameraByUid(EntityUid uid, EntityUid newCamera, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        if (monitor.ActiveCamera == null)
        {
            SetCamera(uid, newCamera, monitor);
        }
        else
        {
            SwitchCamera(uid, newCamera, monitor);
        }
    }

    private void RemoveActiveCamera(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || monitor.ActiveCamera == null)
        {
            return;
        }

        _surveillanceCameras.RemoveActiveViewers(monitor.ActiveCamera.Value, monitor.Viewers, uid);

        UpdateUserInterface(uid, monitor);
    }

    // This is public primarily because it might be useful to have the ability to
    // have this component added to any entity, and have them open the BUI (somehow).
    public void AfterOpenUserInterface(EntityUid uid, EntityUid player, SurveillanceCameraMonitorComponent? monitor = null, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !Resolve(player, ref actor))
        {
            return;
        }

        AddViewer(uid, player);
    }

    private void UpdateUserInterface(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null, EntityUid? player = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        var state = new SurveillanceCameraMonitorUiState(
            GetNetEntity(monitor.ActiveCamera),
            // <Trauma> - replaced the rest of the fields
            monitor.ActiveCameraAddress,
            monitor.KnownCameras,
            monitor.KnownMobileCameras);
            // </Trauma>
        _userInterface.SetUiState(uid, SurveillanceCameraMonitorUiKey.Key, state);
    }
}
