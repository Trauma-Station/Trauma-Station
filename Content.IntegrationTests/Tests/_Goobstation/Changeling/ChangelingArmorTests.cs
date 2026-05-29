// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Changeling.Components;
using Content.IntegrationTests.Fixtures;
using Content.Server.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Goobstation.Changeling;

[TestFixture]
public sealed class ChangelingArmorTest : GameTest
{
    private static readonly EntProtoId mercenaryHelmet = "ClothingHeadHelmetMerc";

    [Test]
    [TestCase("ActionToggleChitinousArmor", "ChangelingClothingOuterArmor", "ChangelingClothingHeadHelmet")]
    [Explicit] // Trauma - takes so long for no benefit idc
    public async Task TestChangelingFullArmor(string actionProto, string outerProto, string helmetProto)
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var timing = server.ResolveDependency<IGameTiming>();

        var actionSys = entMan.System<ActionsSystem>();
        var invSys = entMan.System<InventorySystem>();

        // Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

        var urist = EntityUid.Invalid;
        ChangelingIdentityComponent changelingIdentity;
        Entity<ActionComponent> armorAction = (EntityUid.Invalid, null);

        await server.WaitPost(() =>
        {
            // Spawn a urist
            urist = entMan.SpawnEntity("MobHuman", testMap.GridCoords);

            // Make urist a changeling
            changelingIdentity = entMan.EnsureComponent<ChangelingIdentityComponent>(urist);
            changelingIdentity.TotalAbsorbedEntities += 10;
            changelingIdentity.MaxChemicals = 1000;
            changelingIdentity.Chemicals = 1000;

            // Give urist chitinous armor action
            var armorActionEntityNullable = actionSys.AddAction(urist, actionProto);
            if (armorActionEntityNullable is not { } armorActionEntity)
                return;

            armorAction = (armorActionEntity, entMan.GetComponent<ActionComponent>(armorActionEntity));
            actionSys.SetUseDelay(armorAction, null);

            // Armor up
            actionSys.PerformAction(urist, armorAction);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(invSys.TryGetSlotEntity(urist, "outerClothing", out var outerClothing), Is.True);
                Assert.That(outerClothing, Is.Not.Null);
                Assert.That(entMan.GetComponent<MetaDataComponent>(outerClothing.Value).EntityPrototype!.ID, Is.EqualTo(outerProto));

                Assert.That(invSys.TryGetSlotEntity(urist, "head", out var head));
                Assert.That(head, Is.Not.Null);
                Assert.That(entMan.GetComponent<MetaDataComponent>(head.Value).EntityPrototype!.ID, Is.EqualTo(helmetProto));
            });

            // Armor down
            actionSys.PerformAction(urist, armorAction);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(invSys.TryGetSlotEntity(urist, "outerClothing", out var outerClothing), Is.False);
                Assert.That(entMan.TryGetComponent<MetaDataComponent>(outerClothing, out var meta), Is.False);
                Assert.That(meta?.EntityPrototype, Is.Null);
            });

            Assert.Multiple(() =>
            {
                Assert.That(invSys.TryGetSlotEntity(urist, "head", out var head), Is.False);
                Assert.That(entMan.TryGetComponent<MetaDataComponent>(head, out var meta), Is.False);
                Assert.That(meta?.EntityPrototype, Is.Null);
            });

            // Equip helmet
            var helm = entMan.SpawnEntity(mercenaryHelmet, testMap.GridCoords);
            Assert.That(invSys.TryEquip(urist, helm, "head", force: true));

            // Try to armor up, should fail due to helmet and not equip anything
            actionSys.PerformAction(urist, armorAction);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(invSys.TryGetSlotEntity(urist, "outerClothing", out var outerClothing), Is.False);
                Assert.That(entMan.TryGetComponent<MetaDataComponent>(outerClothing, out var meta), Is.False);
                Assert.That(meta?.EntityPrototype, Is.Null);
            });

            Assert.Multiple(() =>
            {
                Assert.That(invSys.TryGetSlotEntity(urist, "head", out var head));
                Assert.That(head, Is.Not.Null);
                Assert.That(entMan.GetComponent<MetaDataComponent>(head.Value).EntityPrototype!.ID, Is.EqualTo(mercenaryHelmet));
            });
            entMan.DeleteEntity(urist);
        });
    }
}
