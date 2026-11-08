// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Body;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Trauma.Common.Clothing;
using Content.Trauma.Common.Inventory;
using Content.Trauma.Shared.Clothing;
using Content.Trauma.Shared.Clothing.MultiToggleable;
using Content.Trauma.Shared.Clothing.Sealable;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class SealableClothingTest : InteractionTest
{
    private static readonly EntProtoId Urist = "MobHuman";
    private static readonly EntProtoId Modsuit = "ClothingModsuitStandardPowerCell";
    // Use these to test overslotting and preserving suitstorage clothing when toggling modsuit piece
    private static readonly EntProtoId TankHarness = "ClothingOuterVestTank";
    private static readonly EntProtoId OxygenTank = "OxygenTank";
    private static readonly ProtoId<InventorySlotPrototype> Back = "back";
    private static readonly ProtoId<InventorySlotPrototype> OuterClothing = "outerClothing";
    private static readonly ProtoId<InventorySlotPrototype> SuitStorage = "suitstorage";
    private static readonly ProtoId<InventorySlotPrototype> Head = "head";
    // Removing head when modsuit is sealed should break seals
    private static readonly ProtoId<OrganCategoryPrototype> HeadOrgan = "Head";

    protected override string PlayerPrototype => Urist;

    [SidedDependency(Side.Server)] private InventorySystem _inventory = default!;
    [SidedDependency(Side.Server)] private MultiToggleableClothingSystem _toggleable = default!;
    [SidedDependency(Side.Server)] private SealableClothingSystem _sealable = default!;
    [SidedDependency(Side.Server)] private ClothingSystem _clothing = default!;
    [SidedDependency(Side.Server)] private BodySystem _body = default!;

    /// <summary>
    /// Tests modsuit general behavior
    /// </summary>
    [Test]
    public async Task ModsuitTest()
    {
        var coords = SEntMan.GetCoordinates(PlayerCoords);
        var mob = SPlayer;
        var harness = EntityUid.Invalid;
        var tank = EntityUid.Invalid;

        var modsuit = SEntMan.GetEntity(await SpawnTarget(Modsuit));
        var toggleable = SComp<MultiToggleableClothingComponent>(modsuit);
        var control = SComp<SealableClothingControlComponent>(modsuit);

        await Server.WaitPost(() =>
        {
            // Equip everything and toggle modsuit pieces
            Assert.That(_inventory.TryEquip(mob, modsuit, Back, silent: true), "Failed to equip modsuit.");

            harness = SSpawn(TankHarness, coords);
            Assert.That(_inventory.TryEquip(mob, harness, OuterClothing, silent: true), "Failed to equip tank harness.");

            tank = SSpawn(OxygenTank, coords);
            Assert.That(_inventory.TryEquip(mob, tank, SuitStorage, silent: true), "Failed to equip oxygen tank.");

            Assert.That(_toggleable.ToggleClothing(mob, mob, (modsuit, toggleable)), "Failed to toggle clothing after equipping modsuit");

            // Test that toggle message actually works
            foreach (var uid in toggleable.ClothingUids.Keys)
            {
                Assert.That(_clothing.IsEquipped(uid), $"Failed to equip {SToPrettyString(uid)}");
            }

            // Test overslotting over tank harness and preserving o2 tank in suit storage after toggling modsuit pieces
            Assert.That(_inventory.TryGetSlotEntity(mob, OuterClothing, out var outerClothing), "Failed to locate outer clothing on mob");
            STryComp<MultiAttachedClothingComponent>(outerClothing, out var attached);
            Assert.That(attached, Is.Not.Null, "Outer clothing does not have MultiAttachedClothingComponent");
            Assert.That(attached.ClothingContainer.Contains(harness), "Failed to overslot modsuit piece over outer clothing");
            Assert.That(_inventory.TryGetSlotEntity(mob, SuitStorage, out var suitStorage) && suitStorage == tank, "Failed to preserve suit storage item after toggling modsuit piece");

            // Run seal process normally and wait until it is done
            Assert.That(_sealable.TryStartSealToggleProcess((modsuit, control), mob, mob), "Failed to start modsuit seal process");
        });

        await RunSeconds(3);

        await Server.WaitPost(() =>
        {
            // Check if everything sealed correctly, also try unequipping parts and modsuit itself, should fail
            Assert.That(control.IsCurrentlySealed, "Failed to seal modsuit");
            Assert.That(_inventory.TryUnequip(mob, Back, silent: true), Is.False, "Managed to unequip sealed modsuit");
            foreach (var (uid, slot) in toggleable.ClothingUids)
            {
                var sealable = SComp<SealableClothingComponent>(uid);
                Assert.That(sealable.IsSealed, $"Failed to seal modsuit part {SToPrettyString(uid)}");
                Assert.That(_inventory.TryUnequip(mob, slot, silent: true), Is.False, $"Managed to unequip sealed modsuit part {SToPrettyString(uid)}");
            }

            // Toggle clothing shouldn't work either
            Assert.That(_toggleable.ToggleClothing(mob, mob, (modsuit, toggleable)), Is.False, "Managed to toggle sealed modsuit parts");

            var clothingUids = toggleable.ClothingUids.Keys.ToList();

            Assert.That(_inventory.TryGetSlotEntity(mob, Head, out var helmet), "Failed to locate helmet on mob");

            // Remove mob's head - seals should break
            var head = _body.GetOrgan(mob, HeadOrgan);
            Assert.That(head, Is.Not.Null, "Failed to find head on mob");
            _body.RemoveOrgan(mob, head.Value);

            Assert.That(toggleable.ClothingUids, Has.Count.EqualTo(clothingUids.Count), "Modsuit piece count changed, helmet was deleted after removing head?");
            Assert.That(control.IsCurrentlySealed, Is.False, "Modsuit seals failed to break after body part was removed");

            // Removing head should break seals but shouldn't prevent from sealing modsuit again
            Assert.That(_sealable.EndSealProcess((modsuit, control), mob, true, true), "Failed to seal modsuit after removing body part");
            Assert.That(toggleable.Container.Contains(helmet.Value), "Helmet is outside of modsuit container after removing head");

            SDel(head.Value);
            SDel(tank);
            SDel(harness);
            SDel(modsuit);
            foreach (var uid in clothingUids)
            {
                Assert.That(SDeleted(uid) || SEntMan.IsQueuedForDeletion(uid), $"{SToPrettyString(uid)} didn't get deleted after modsuit deletion");
            }
        });
    }
}
