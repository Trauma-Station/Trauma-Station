// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Traitor;
using Content.Goobstation.Server.Traitor.PenSpin;
using Content.Goobstation.Shared.Traitor.PenSpin;
using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Server.Traitor.Uplink;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.PDA;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Goobstation;

[TestFixture]
public sealed class UplinkPreferenceTests : GameTest
{
    private EntityUid _player;

    private static readonly ProtoId<UplinkPreferencePrototype> PenPreference = "UplinkPen";
    private static readonly ProtoId<ListingPrototype> ImplanterListing = "UplinkUplinkImplanter";

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true
    };

    public override async Task DoSetup()
    {
        await base.DoSetup();
        var ticker = SEntMan.System<GameTicker>();

        await Pair.Server.WaitPost(() =>
        {
            ticker.ToggleReadyAll(true);
            ticker.StartRound();
        });
        await RunTicksSync(10);

        _player = Pair.Player!.AttachedEntity!.Value;
    }

    private async Task<EntityUid> SpawnPenInHand()
    {
        var server = Pair.Server;
        var handsSys = SEntMan.System<SharedHandsSystem>();

        EntityUid pen = default;
        await server.WaitPost(() =>
        {
            var coords = SEntMan.GetComponent<TransformComponent>(_player).Coordinates;
            pen = SEntMan.SpawnEntity("Pen", coords);
            handsSys.TryPickupAnyHand(_player, pen);
        });
        await RunTicksSync(5);

        return pen;
    }

    [Test]
    public async Task TestFindPdaUplinkTarget()
    {
        var server = Pair.Server;
        var goobUplinkSys = SEntMan.System<GoobCommonUplinkSystem>();

        await server.WaitAssertion(() =>
        {
            var pdaTarget = goobUplinkSys.FindUplinkTarget(_player, new[] { "Pda" });
            Assert.That(pdaTarget, Is.Not.Null, "Player should have a PDA");
            Assert.That(SEntMan.HasComponent<PdaComponent>(pdaTarget!.Value), Is.True);
        });
    }

    [Test]
    public async Task TestFindPenUplinkTarget()
    {
        var server = Pair.Server;
        var goobUplinkSys = SEntMan.System<GoobCommonUplinkSystem>();

        await SpawnPenInHand();

        await server.WaitAssertion(() =>
        {
            var penTarget = goobUplinkSys.FindUplinkTarget(_player, new[] { "Pen" });
            Assert.That(penTarget, Is.Not.Null, "Player should have a pen");
            Assert.That(SEntMan.HasComponent<PenComponent>(penTarget!.Value), Is.True);
        });
    }

    [Test]
    public async Task TestPenUplinkCodeGeneration()
    {
        var server = Pair.Server;
        var uplinkSys = SEntMan.System<UplinkSystem>();
        var goobUplinkSys = SEntMan.System<GoobCommonUplinkSystem>();

        await SpawnPenInHand();

        await server.WaitPost(() => uplinkSys.AddUplink(_player, 20, PenPreference, out _, out _));
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var penTarget = goobUplinkSys.FindUplinkTarget(_player, new[] { "Pen" });
            Assert.That(penTarget, Is.Not.Null);

            var spinComp = SEntMan.GetComponent<PenComponent>(penTarget!.Value);
            Assert.That(spinComp.CombinationLength, Is.EqualTo(4));
            Assert.That(spinComp.MinDegree, Is.EqualTo(0));
            Assert.That(spinComp.MaxDegree, Is.EqualTo(359));

            var uplinkComp = SEntMan.GetComponent<PenSpinUplinkComponent>(penTarget.Value);
            Assert.That(uplinkComp.Code, Is.Not.Null);
            Assert.That(uplinkComp.Code!.Length, Is.EqualTo(4));

            foreach (var degree in uplinkComp.Code)
            {
                Assert.That(degree, Is.GreaterThanOrEqualTo(0));
                Assert.That(degree, Is.LessThanOrEqualTo(359));
            }
        });
    }

    [Test]
    public async Task TestAllUplinkPreferences()
    {
        var server = Pair.Server;
        var uplinkSys = SEntMan.System<UplinkSystem>();
        var handsSys = SEntMan.System<SharedHandsSystem>();

        foreach (var pref in SProtoMan.EnumeratePrototypes<UplinkPreferencePrototype>())
        {
            if (pref.SearchComponents != null)
            {
                await server.WaitPost(() =>
                {
                    if (Array.IndexOf(pref.SearchComponents, "Pen") >= 0)
                    {
                        var coords = SEntMan.GetComponent<TransformComponent>(_player).Coordinates;
                        var pen = SEntMan.SpawnEntity("Pen", coords);
                        handsSys.TryPickupAnyHand(_player, pen);
                    }
                });
                await RunTicksSync(5);
            }

            await server.WaitAssertion(() =>
            {
                var success = uplinkSys.AddUplink(_player, 20, pref.ID, out var uplinkTarget, out var setupEvent);
                Assert.That(success, Is.True, $"TryAddUplink failed for preference {pref.ID}");

                if (pref.SearchComponents != null)
                {
                    Assert.That(uplinkTarget, Is.Not.Null, $"Should find uplink target for {pref.ID}");
                    Assert.That(setupEvent, Is.Not.Null, $"SetupUplinkEvent should be raised for {pref.ID}");
                    Assert.That(setupEvent!.Value.Handled, Is.True, $"SetupUplinkEvent should be handled for {pref.ID}");

                    var store = SEntMan.GetComponent<StoreComponent>(uplinkTarget!.Value);
                    Assert.That(store.Balance.ContainsKey("Telecrystal"), Is.True, $"Store should have TC for {pref.ID}");
                    Assert.That((int) store.Balance["Telecrystal"], Is.EqualTo(20), $"Store should have 20 TC for {pref.ID}");
                }
                else // Fallback
                {
                    Assert.That(uplinkTarget, Is.Null, $"Implant preference {pref.ID} should have no target entity");
                    Assert.That(setupEvent, Is.Null, $"Implant preference {pref.ID} should have no setup event");
                }
            });
        }
    }

    [Test]
    public async Task TestImplantUplinkBalance()
    {
        var server = Pair.Server;
        var uplinkSys = SEntMan.System<UplinkSystem>();

        const int startingBalance = 20;
        var implantPreference = new ProtoId<UplinkPreferencePrototype>("UplinkImplant");

        await server.WaitAssertion(() =>
        {
            var success = uplinkSys.AddUplink(_player, startingBalance, implantPreference, out var uplinkTarget, out _);
            Assert.That(success, Is.True, "Implant uplink should succeed");
            Assert.That(uplinkTarget, Is.Null, "Implant preference should not return an uplink target entity");

            Assert.That(SEntMan.TryGetComponent<ImplantedComponent>(_player, out var implanted), Is.True,
                "Player should have ImplantedComponent after implant uplink");

            EntityUid? implantStore = null;
            foreach (var implantEnt in implanted!.ImplantContainer.ContainedEntities)
            {
                if (SEntMan.HasComponent<StoreComponent>(implantEnt))
                {
                    implantStore = implantEnt;
                    break;
                }
            }

            Assert.That(implantStore, Is.Not.Null, "Should find a store component on the implant");

            var store = SEntMan.GetComponent<StoreComponent>(implantStore!.Value);
            Assert.That(store.Balance.ContainsKey("Telecrystal"), Is.True);

            var catalog = SProtoMan.Index(ImplanterListing);
            var implantCost = (int) catalog.Cost["Telecrystal"];
            var expectedBalance = startingBalance - implantCost;
            Assert.That((int) store.Balance["Telecrystal"], Is.EqualTo(expectedBalance),
                $"Implant store should have {expectedBalance} TC (starting {startingBalance} minus {implantCost} implant cost)");
        });
    }

    [Test]
    public async Task TestAddUplinkFallbackToImplant()
    {
        var server = Pair.Server;
        var uplinkSys = SEntMan.System<UplinkSystem>();
        var goobUplinkSys = SEntMan.System<GoobCommonUplinkSystem>();

        EntityUid dummy = default;
        await server.WaitPost(() =>
        {
            dummy = SEntMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var penTarget = goobUplinkSys.FindUplinkTarget(dummy, new[] { "Pen" });
            Assert.That(penTarget, Is.Null, "Dummy should not have a pen");

            var success = uplinkSys.AddUplink(dummy, 20, PenPreference, out _, out _);
            Assert.That(success, Is.True, "Should fall back to implant when pen unavailable");
        });
    }
}
