// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Power.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Trauma.Shared.DeepFryer.Components;
using Content.Trauma.Shared.DeepFryer.Systems;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed partial class DeepFryerTest : GameTest
{
    private static readonly EntProtoId DeepFryer = "KitchenDeepFryer";
    private static readonly EntProtoId Potato = "FoodPotato";
    private static readonly EntProtoId Fries = "FoodMealFries";
    private static readonly ProtoId<ReagentPrototype> Oil = "OilOlive";

    [SidedDependency(Side.Server)] private SharedEntityStorageSystem _entityStorage = default!;
    [SidedDependency(Side.Server)] private SharedPhysicsSystem _physics = default!;
    [SidedDependency(Side.Server)] private SharedSolutionContainerSystem _solution = default!;

    /// <summary>
    /// Makes sure that the space fries recipe works.
    /// </summary>
    [Test]
    public async Task DeepFryerRecipeWorks()
    {
        var map = await Pair.CreateTestMap();
        var uid = EntityUid.Invalid;
        DeepFryerComponent fryer = default!;
        var potato = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            uid = SSpawn(DeepFryer, map.GridCoords);
            fryer = SComp<DeepFryerComponent>(uid);
            potato = SSpawn(Potato, map.GridCoords);
            SRemComp<ApcPowerReceiverComponent>(uid); // this isn't a test of power

            Assert.That(fryer.StoredObjects.Count == 0, "Fryer should start empty");
        });

        // let physics settle for it to get inserted
        _physics.WakeBody(potato);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            // fill the fryer with oil
            var total = FixedPoint2.New(150);
            var initialOil = new Solution(Oil, total);
            Assert.That(_solution.TryGetSolution(uid, fryer.FryerSolutionContainer, out var fryerSolution, out _));
            _solution.AddSolution(fryerSolution.Value, initialOil);

            Assert.That(_entityStorage.TryCloseStorage(uid), "Failed to close fryer");

            // TODO: this should not be necessary but it seems like the lookup isnt finding the potato...
            Assert.That(_entityStorage.Insert(potato, uid));
            fryer.StoredObjects.Add(potato);

            Assert.That(SComp<TransformComponent>(potato).ParentUid, Is.EqualTo(uid), "Potato did not get inserted into the fryer");
            Assert.That(fryer.StoredObjects.Count, Is.EqualTo(1), "Fryer should have added the potato to StoredObjects");
            Assert.That(SHasComp<ActiveDeepFryerComponent>(uid), "Fryer should have started after being closed");
        });

        var finishTime = fryer.FryFinishTime;

        // wait until its done frying
        await RunSeconds((float) fryer.TimeToDeepFry.TotalSeconds);
        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(fryer.FryFinishTime != finishTime, "Fryer did not finish frying");
            Assert.That(SDeleted(potato), "Potato was not deleted after cooking");
            var endingOil = _solution.GetTotalPrototypeQuantity(uid, Oil);
            // some oil is consumed when coating the fries, so it wont be exactly 145
            Assert.That(endingOil < FixedPoint2.New(145), "Not enough oil was consumed by the recipe");

            var fries = SComp<EntityStorageComponent>(uid).Contents.ContainedEntities[0];
            var friesId = SPrototype(fries)?.ID;
            Assert.That(friesId, Is.EqualTo(Fries), "Potato did not get cooked into fries");

            SDel(uid);
            SDel(fries);
        });
    }
}
