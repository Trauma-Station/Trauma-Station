// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Tests.Interaction;
using Content.Server.SurveillanceCamera;
using Content.Shared.Coordinates;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Shared.Map;
using System.Text;

namespace Content.IntegrationTests.Tests._Trauma;

// A surveillance camera?!
public sealed partial class SurveillanceCameraTest : InteractionTest
{
    private static readonly EntProtoId Apc = "DebugAPCRecharging";
    private static readonly new EntProtoId Cable = "CableApcExtension";
    private static readonly EntProtoId Camera = "SurveillanceCameraSecurity";
    private static readonly EntProtoId Router = "SurveillanceCameraRouterSecurity";
    private static readonly EntProtoId Monitor = "WallmountTelescreen";
    private static readonly ProtoId<DeviceFrequencyPrototype> Frequency = "SurveillanceCameraSecurity";
    private const string CameraName = "test camera";

    [SidedDependency(Side.Server)] private SharedMapSystem _map = default!;
    [SidedDependency(Side.Server)] private SharedPowerReceiverSystem _power = default!;
    [SidedDependency(Side.Server)] private SharedSurveillanceCameraSystem _camera = default!;

    /// <summary>
    /// Checks that a camera is able to be viewed from a camera monitor with a router.
    /// </summary>
    [Test]
    public async Task CameraWorksTest()
    {
        var camera = EntityUid.Invalid;
        var router = EntityUid.Invalid;
        var monitor = EntityUid.Invalid;
        await Server.WaitPost(() =>
        {
            var grid = MapData.Grid;
            var gridUid = grid.Owner;
            var tile = new Tile(1);
            _map.SetTile(grid, new Vector2i(0, 1), tile);
            _map.SetTile(grid, new Vector2i(1, 0), tile);
            _map.SetTile(grid, new Vector2i(0, -1), tile);
            SEntMan.SpawnAttachedTo(Apc, MapData.GridCoords);
            SEntMan.SpawnAttachedTo(Cable, MapData.GridCoords);
            camera = SEntMan.SpawnAttachedTo(Camera, gridUid.ToCoordinates(0, 1));
            router = SEntMan.SpawnAttachedTo(Router, gridUid.ToCoordinates(1, 0));
            monitor = SEntMan.SpawnAttachedTo(Monitor, gridUid.ToCoordinates(0, -1));
        });
        var netCamera = SEntMan.GetNetEntity(camera);
        var netMonitor = SEntMan.GetNetEntity(monitor);

        await RunSeconds(5); // let their power stabilize
        await Server.WaitAssertion(() =>
        {
            // they can't work without power
            Assert.That(_power.IsPowered(camera));
            Assert.That(_power.IsPowered(router));
            Assert.That(_power.IsPowered(monitor));

            _camera.OpenSetupInterface(camera, SPlayer);
        });

        // set the camera up
        await RunTicksSync(15);
        var camKey = SurveillanceCameraSetupUiKey.Camera;
        await SendBui(camKey, new SurveillanceCameraSetupSetName(CameraName), netCamera);

        var cameraComp = SComp<SurveillanceCameraComponent>(camera);
        var cameraAddr = SComp<DeviceNetworkComponent>(camera).Address;
        Assert.That(cameraComp.NameSet, "Setup camera UI didn't change the name");
        Assert.That(cameraComp.CameraId, Is.EqualTo(CameraName), "Setup camera UI didn't set the expected name");
        Assert.That(cameraAddr, Is.Not.Empty, "Camera didn't have an address set");

        // monitor shouldn't be linked to them already
        var routerComp = SComp<SurveillanceCameraRouterComponent>(router);
        Assert.That(routerComp.SubnetFrequencyId, Is.EqualTo(Frequency));
        var monitorComp = SComp<SurveillanceCameraMonitorComponent>(monitor);
        Assert.That(monitorComp.ActiveCamera, Is.Null);
        Assert.That(monitorComp.KnownCameras, Is.Empty);
        Assert.That(monitorComp.KnownSubnets, Is.Empty);

        var routerAddr = SComp<DeviceNetworkComponent>(router).Address;
        Assert.That(routerAddr, Is.Not.Empty, "Router didn't have an address set");

        // open the monitor's UI and refresh subnets.
        var monKey = SurveillanceCameraMonitorUiKey.Key; // OOK!
        await Activate(netMonitor);
        await RunTicksSync(15);
        await SendBui(monKey, new SurveillanceCameraRefreshSubnetsMessage(), netMonitor);
        await Server.WaitAssertion(() =>
        {
            var subnets = monitorComp.KnownSubnets;
            Assert.That(subnets.ContainsKey(Frequency), "Refreshing subnets didn't find the camera router!");
            Assert.That(subnets[Frequency], Is.EqualTo(routerAddr), "The located camera router had the wrong address!");
        });

        // now refresh cameras
        await SendBui(monKey, new SurveillanceCameraRefreshCamerasMessage(), netMonitor);
        await RunSeconds(5);
        await Server.WaitAssertion(() =>
        {
            var cameras = monitorComp.KnownCameras;
            Assert.That(cameras.ContainsKey(cameraAddr), "Refreshing cameras didn't find the camera");
            Assert.That(cameras[cameraAddr].Item1, Is.EqualTo(CameraName));
            Assert.That(cameras[cameraAddr].Item2.Item1, Is.EqualTo(netCamera));
        });
    }
}
