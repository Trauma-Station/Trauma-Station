// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Server.Power.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Trauma.Shared.DeepFryer.Components;
using Content.Trauma.Shared.DeepFryer.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class DeepFryerTest : GameTest
{
    public static readonly EntProtoId DeepFryer = "KitchenDeepFryer";
    public static readonly EntProtoId Potato = "FoodPotato";
    public static readonly EntProtoId Fries = "FoodMealFries";
    public static readonly ProtoId<ReagentPrototype> Oil = "OilOlive";

    /// <summary>
    /// Makes sure that the space fries recipe works.
    /// </summary>
    [Test]
    public async Task DeepFryerRecipeWorks()
    {
        var pair = Pair;
        var server = pair.Server;

        var entMan = server.EntMan;
        var storage = entMan.System<SharedEntityStorageSystem>();
        var physics = entMan.System<SharedPhysicsSystem>();
        var solution = entMan.System<SharedSolutionContainerSystem>();

        var map = await pair.CreateTestMap();
        var uid = EntityUid.Invalid;
        DeepFryerComponent fryer = default!;
        var potato = EntityUid.Invalid;

        await server.WaitAssertion(() =>
        {
            uid = entMan.SpawnAtPosition(DeepFryer, map.GridCoords);
            fryer = entMan.GetComponent<DeepFryerComponent>(uid);
            potato = entMan.SpawnAtPosition(Potato, map.GridCoords);
            entMan.RemoveComponent<ApcPowerReceiverComponent>(uid); // this isn't a test of power

            Assert.That(fryer.StoredObjects.Count == 0, "Fryer should start empty");
        });

        // let physics settle for it to get inserted
        physics.WakeBody(potato);
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            // fill the fryer with oil
            var total = FixedPoint2.New(150);
            var initialOil = new Solution(Oil, total);
            Assert.That(solution.TryGetSolution(uid, fryer.FryerSolutionContainer, out var fryerSolution, out _));
            solution.AddSolution(fryerSolution.Value, initialOil);

            Assert.That(storage.TryCloseStorage(uid), "Failed to close fryer");

            // TODO: this should not be necessary but it seems like the lookup isnt finding the potato...
            Assert.That(storage.Insert(potato, uid));
            fryer.StoredObjects.Add(potato);

            Assert.That(entMan.GetComponent<TransformComponent>(potato).ParentUid, Is.EqualTo(uid), "Potato did not get inserted into the fryer");
            Assert.That(fryer.StoredObjects.Count, Is.EqualTo(1), "Fryer should have added the potato to StoredObjects");
            Assert.That(entMan.HasComponent<ActiveDeepFryerComponent>(uid), "Fryer should have started after being closed");
        });

        var finishTime = fryer.FryFinishTime;

        // wait until its done frying
        await pair.RunSeconds((float) fryer.TimeToDeepFry.TotalSeconds);
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(fryer.FryFinishTime != finishTime, "Fryer did not finish frying");
            Assert.That(entMan.Deleted(potato), "Potato was not deleted after cooking");
            var endingOil = solution.GetTotalPrototypeQuantity(uid, Oil);
            // some oil is consumed when coating the fries, so it wont be exactly 145
            Assert.That(endingOil < FixedPoint2.New(145), "Not enough oil was consumed by the recipe");

            var fries = entMan.GetComponent<EntityStorageComponent>(uid).Contents.ContainedEntities[0];
            var friesId = entMan.GetComponent<MetaDataComponent>(fries).EntityPrototype?.ID;
            Assert.That(friesId, Is.EqualTo(Fries), "Potato did not get cooked into fries");

            entMan.DeleteEntity(uid);
            entMan.DeleteEntity(fries);
        });
    }
}
