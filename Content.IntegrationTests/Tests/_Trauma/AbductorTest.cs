// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Shared.Abductor;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Power.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Makes sure abductor gamerules work.
/// </summary>
[TestFixture]
public sealed class AbductorTest : InteractionTest
{
    public static EntProtoId Beacon = "DefaultStationBeacon";
    public static EntProtoId Gizmo = "AbductorGizmo";
    public static EntProtoId LoneRule = "LoneAbductorSpawn";
    public static EntProtoId LoneMob = "MobLoneAbductor";
    public static EntProtoId Victim = "MobHuman";

    protected override string PlayerPrototype => LoneMob;

    /// <summary>
    /// Tests the lone abductor antag loop.
    /// Makes sure you can:
    /// 0. load the gamerule
    /// 1. teleport alien to the station
    /// 2. gizmo a urist
    /// 3. teleport back to your ship
    /// 4. abduct the urist
    /// 5. send the urist back
    /// </summary>
    /// <remarks>
    /// Caveats:
    /// It is assumed the ghost role spawner works, this just spawns a mob manually.
    /// Power is ignored.
    /// Surgery should be tested separately.
    /// The shittle just gets teleported onto the map, FTL should be tested separately.
    /// </remarks>
    [Test]
    public async Task LoneAbductorWorks()
    {
        var ticker = SEntMan.System<GameTicker>();
        var actions = SEntMan.System<SharedActionsSystem>();

        var alien = SPlayer;
        var rule = EntityUid.Invalid;
        var shittle = EntityUid.Invalid;
        await Server.WaitPost(() =>
        {
            rule = ticker.AddGameRule(LoneRule);

            // skipping ftl and just teleporting it to the "station" map, in space far away
            shittle = SEntMan.GetComponent<RuleGridsComponent>(rule).MapGrids[0];
            Transform.SetCoordinates(shittle, new EntityCoordinates(MapData.MapUid, new Vector2(500, 0)));

            // prevent colliding with the consoles
            var fixtures = SEntMan.GetComponent<FixturesComponent>(SPlayer);
            fixtures.Fixtures.Clear();
            SEntMan.Dirty(SPlayer, fixtures);

            var scientist = SEntMan.GetComponent<AbductorScientistComponent>(SPlayer);
            scientist.SpawnPosition = new EntityCoordinates(shittle, Vector2.Zero);
        });

        var xform = SEntMan.GetComponent<TransformComponent>(SPlayer);

        // get the consoles we need
        var (console, consoleNet) = FindEntityWith<AbductorConsoleComponent>();
        var (teleporter, teleNet) = FindEntityWith<AbductorHumanObservationConsoleComponent>();
        var (_, podNet) = FindEntityWith<AbductorExperimentatorComponent>();
        var consoleCoords = SEntMan.GetComponent<TransformComponent>(console).Coordinates;

        // this isn't a test of pow3r, they prevent the UIs working and this is easy
        await Server.WaitPost(() =>
        {
            SEntMan.RemoveComponent<ActivatableUIRequiresPowerComponent>(console);
            SEntMan.RemoveComponent<ActivatableUIRequiresPowerComponent>(teleporter);
        });

        // spawn our victim and a beacon for him
        var coords = SEntMan.GetNetCoordinates(MapData.GridCoords);
        var urist = SEntMan.GetEntity(await SpawnTarget(Victim, coords));
        var beacon = await Spawn(Beacon, coords);
        // put our brave soldier in interaction range of the console
        Transform.PlaceNextTo(SPlayer, teleporter.Owner);

        // make sure everything is happy
        await RunTicks(15);

        // 1. open the console, teleport to the urist
        await Activate(teleNet);
        Assert.That(IsUiOpen(AbductorCameraConsoleUIKey.Key),
            $"Alien {xform.Coordinates} vs {SEntMan.GetComponent<TransformComponent>(teleporter).Coordinates}!");
        await SendBui(AbductorCameraConsoleUIKey.Key, new AbductorBeaconChosenBuiMsg(beacon), teleNet);

        await RunTicks(3);

        // player is now controlling the eye, teleport
        Assert.That(SEntMan.HasComponent<RelayInputMoverComponent>(SPlayer), "Abductor did not control an eye entity");
        var abilities = SEntMan.GetComponent<AbductorsAbilitiesComponent>(SPlayer);
        Assert.That(abilities.SendYourself != null, "Abductor did not get a send yourself action");
        // shitcode to mimic the action because non-instant actions api is bad
        await Server.WaitPost(() =>
        {
            // TODO: change it to not be broadcast bruh
            var ev = new SendYourselfEvent()
            {
                Performer = SPlayer,
                Target = MapData.GridCoords
            };
            SEntMan.EventBus.RaiseLocalEvent(SPlayer, ev, broadcast: true);
            Assert.That(ev.Handled, $"Abductor send yourself action was not handled by anything");
        });

        // wait for the teleport
        await AwaitDoAfters();
        await RunTicks(1);

        Assert.That(xform.GridUid, Is.EqualTo(MapData.Grid.Owner), $"Abductor failed to teleport to the station, currently at {Transform.GetWorldPosition(SPlayer)}!");

        // 2. equip a gizmo, use it on the urist
        var gizmoNet = await Spawn(Gizmo, SEntMan.GetNetCoordinates(xform.Coordinates));
        var gizmo = SEntMan.GetEntity(gizmoNet);
        await Server.WaitPost(() =>
        {
            Assert.That(HandSys.TryPickupAnyHand(SPlayer, gizmo), $"Abductor failed to pick up its gizmo!");
        });
        await Interact(); // nyoom
        Assert.That(SEntMan.GetComponent<AbductorGizmoComponent>(gizmo).Target, Is.EqualTo(Target),
            $"Using gizmo on a urist didn't set him as the target");

        // 3. teleport back to ship
        var action = EntityUid.Invalid;
        await Server.WaitPost(() =>
        {
            // TODO: this is extreme shitcode and should just be stored on an abductor component
            action = SEntMan.GetComponent<ActionGrantComponent>(SPlayer).ActionEntities[0];
            actions.PerformAction(SPlayer, (action, SEntMan.GetComponent<ActionComponent>(action)), predicted: false);
        });
        await AwaitDoAfters();
        await RunTicks(3);
        Assert.That(xform.GridUid, Is.EqualTo(shittle),
            $"Abductor failed to return to ship using action {SEntMan.ToPrettyString(action)}, currently at {Transform.GetWorldPosition(SPlayer)}!");

        // 4. abduct the urist: link the target to abductor console
        Assert.That(console.Comp.Target, Is.Null,
            $"Console shouldn't be linked yet");
        await Interact(consoleNet, SEntMan.GetNetCoordinates(consoleCoords), false);
        Assert.That(console.Comp.Target, Is.EqualTo(Target),
            $"Using linked gizmo on console didn't set its target");

        // then teleport urist to the ship
        Transform.PlaceNextTo(SPlayer, console.Owner);
        await Activate(consoleNet);
        Assert.That(IsUiOpen(AbductorConsoleUIKey.Key));
        Assert.That(console.Comp.AlienPod, Is.Not.Null);
        await SendBui(AbductorConsoleUIKey.Key, new AbductorAttractBuiMsg(), consoleNet);
        await CloseBui(AbductorConsoleUIKey.Key, consoleNet);
        await AwaitDoAfters();

        var uristXform = SEntMan.GetComponent<TransformComponent>(urist);
        Assert.That(uristXform.GridUid, Is.EqualTo(shittle),
            $"Failed to teleport urist using abductor console, currently at {Transform.GetWorldPosition(urist)}!");

        // 5. put urist in the pod and send him home
        await DragDrop(Target.Value, podNet);
        await AwaitDoAfters();
        await RunTicks(2);

        await Activate(consoleNet);
        Assert.That(IsUiOpen(AbductorConsoleUIKey.Key));
        await SendBui(AbductorConsoleUIKey.Key, new AbductorCompleteExperimentBuiMsg(), consoleNet);
        await CloseBui(AbductorConsoleUIKey.Key, consoleNet);
        await RunTicks(2);
        Assert.That(uristXform.GridUid, Is.EqualTo(MapData.Grid.Owner),
            $"Failed to teleport urist back to the station using abductor console!");

        // so long and thanks for all the fish
        await Delete(urist);
        await Delete(shittle);
        await Delete(rule);
    }

    private (Entity<T>, NetEntity) FindEntityWith<T>() where T: Component
    {
        var query = SEntMan.EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            return ((uid, comp), SEntMan.GetNetEntity(uid));
        }

        throw new Exception($"No entity found with {typeof(T).Name} found!");
    }
}
