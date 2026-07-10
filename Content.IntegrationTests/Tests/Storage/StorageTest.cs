#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Containers;
using Content.Shared.Item;
using Content.Shared.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Storage;

public sealed class StorageTest : GameTest
{
    /// <summary>
    /// Can an item store more than itself weighs.
    /// In an ideal world this test wouldn't need to exist because sizes would be recursive.
    /// </summary>
    [Test]
    public async Task StorageSizeArbitrageTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var entMan = server.ResolveDependency<IEntityManager>();
        // <Trauma> - microoptimisation
        var compFact = server.EntMan.ComponentFactory;
        var itemName = compFact.CompName<ItemComponent>();
        var storageName = compFact.CompName<StorageComponent>();
        // </Trauma>

        var itemSys = entMan.System<SharedItemSystem>();

        await server.WaitAssertion(() =>
        {
            foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryComp<StorageComponent>(storageName, out var storage) || // Trauma - use storageName
                    storage.Whitelist != null ||
                    storage.MaxItemSize == null ||
                    !proto.TryComp<ItemComponent>(itemName, out var item)) // Trauma - use itemName
                    continue;

                Assert.That(itemSys.GetSizePrototype(storage.MaxItemSize.Value).Weight,
                    Is.LessThanOrEqualTo(itemSys.GetSizePrototype(item.Size).Weight),
                    $"Found storage arbitrage on {proto.ID}");
            }
        });
    }

    [Test]
    public async Task TestStorageFillPrototypes()
    {
        var pair = Pair;
        var server = pair.Server;

        // <Trauma> - microoptimisation
        var protoManager = server.ProtoMan;
        var compFact = server.EntMan.ComponentFactory;
        var fillName = compFact.CompName<StorageFillComponent>();
        // </Trauma>

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryComp<StorageFillComponent>(fillName, out var storage)) // Trauma - use fillName
                        continue;

                    foreach (var entry in storage.Contents)
                    {
                        Assert.That(entry.Amount, Is.GreaterThan(0), $"Specified invalid amount of {entry.Amount} for prototype {proto.ID}");
                        Assert.That(entry.SpawnProbability, Is.GreaterThan(0), $"Specified invalid probability of {entry.SpawnProbability} for prototype {proto.ID}");
                    }
                }
            });
        });
    }

    [Test]
    public async Task TestSufficientSpaceForFill()
    {
        var pair = Pair;
        var server = pair.Server;

        // <Trauma> - microoptimisation
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var compFact = entMan.ComponentFactory;
        var storageName = compFact.CompName<EntityStorageComponent>();
        var itemName = compFact.CompName<ItemComponent>();
        var groups = new Dictionary<string, int>();
        // </Trauma>

        var itemSys = entMan.System<SharedItemSystem>();

        var allSizes = protoMan.EnumeratePrototypes<ItemSizePrototype>().ToList();
        allSizes.Sort();

        await Assert.MultipleAsync(async () =>
        {
            foreach (var (proto, fill) in pair.GetPrototypesWithComponent<StorageFillComponent>())
            {
                if (proto.Components.ContainsKey(storageName)) // Trauma - use cached name instead of slop
                    continue;

                StorageComponent? storage = null;
                ItemComponent? item = null;
                var size = 0;
                await server.WaitAssertion(() =>
                {
                    if (!proto.TryComp(out storage, compFact))
                    {
                        Assert.Fail($"Entity {proto.ID} has storage-fill without a storage component!");
                        return;
                    }

                    proto.TryComp(itemName, out item); // Trauma - use itemName
                    size = GetFillSize(fill, false, protoMan, itemName, itemSys, groups); // Trauma - replace compFact with itemName, pass groups
                });

                if (storage == null)
                    continue;

                var maxSize = storage.MaxItemSize;
                if (storage.MaxItemSize == null)
                {
                    if (item?.Size == null)
                    {
                        maxSize = SharedStorageSystem.DefaultStorageMaxItemSize;
                    }
                    else
                    {
                        var curIndex = allSizes.IndexOf(protoMan.Index(item.Size));
                        var index = Math.Max(0, curIndex - 1);
                        maxSize = allSizes[index].ID;
                    }
                }

                if (maxSize == null)
                    continue;

                Assert.That(size, Is.LessThanOrEqualTo(storage.Grid.GetArea()), $"{proto.ID} storage fill is too large.");

                foreach (var entry in fill.Contents)
                {
                    if (entry.PrototypeId == null)
                        continue;

                    if (!protoMan.TryIndex<EntityPrototype>(entry.PrototypeId, out var fillItem))
                        continue;

                    ItemComponent? entryItem = null;
                    await server.WaitPost(() =>
                    {
                        fillItem.TryComp(out entryItem, compFact);
                    });

                    if (entryItem == null)
                        continue;

                    Assert.That(protoMan.Index(entryItem.Size).Weight,
                        Is.LessThanOrEqualTo(protoMan.Index(maxSize.Value).Weight),
                        $"Entity {proto.ID} has storage-fill item, {entry.PrototypeId}, that is too large");
                }
            }
        });
    }

    [Test]
    public async Task TestSufficientSpaceForEntityStorageFill()
    {
        var pair = Pair;
        var server = pair.Server;

        // <Trauma> - microoptimisation
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var compFact = entMan.ComponentFactory;
        var entStorageName = compFact.CompName<EntityStorageComponent>();
        var itemName = compFact.CompName<ItemComponent>();
        var storageName = compFact.CompName<StorageComponent>();
        var groups = new Dictionary<string, int>();
        // </Trauma>

        var itemSys = entMan.System<SharedItemSystem>();

        foreach (var (proto, fill) in pair.GetPrototypesWithComponent<StorageFillComponent>())
        {
            if (proto.HasComp(storageName)) // Trauma - use cached name instead of slop
                continue;

            await server.WaitAssertion(() =>
            {
                if (!proto.TryComp(entStorageName, out EntityStorageComponent? entStorage)) // Trauma - use entStorageName
                    Assert.Fail($"Entity {proto.ID} has storage-fill without a storage component!");

                if (entStorage == null)
                    return;

                var size = GetFillSize(fill, true, protoMan, itemName, itemSys, groups); // Trauma - replace compFact with itemName, pass groups
                Assert.That(size, Is.LessThanOrEqualTo(entStorage.Capacity),
                    $"{proto.ID} storage fill is too large.");
            });
        }
    }

    private int GetEntrySize(EntitySpawnEntry entry, bool getCount, IPrototypeManager protoMan, CompName itemName, SharedItemSystem itemSystem) // Trauma - replace compFact with itemName
    {
        if (entry.PrototypeId == null)
            return 0;

        if (!protoMan.TryIndex<EntityPrototype>(entry.PrototypeId, out var proto))
        {
            Assert.Fail($"Unknown prototype: {entry.PrototypeId}");
            return 0;
        }

        if (getCount)
            return entry.Amount;


        if (proto.TryComp<ItemComponent>(itemName, out var item)) // Trauma - use itemName
            return itemSystem.GetItemShape(item).GetArea() * entry.Amount;

        Assert.Fail($"Prototype is missing item comp: {entry.PrototypeId}");
        return 0;
    }

    // Trauma - replace compFact with itemName, added groups
    private int GetFillSize(StorageFillComponent fill, bool getCount, IPrototypeManager protoMan, CompName itemName, SharedItemSystem itemSystem, Dictionary<string, int> groups)
    {
        var totalSize = 0;
        // <Trauma> - zero out instead of allocating new dict
        foreach (var size in groups.Keys)
        {
            groups[size] = 0;
        }
        // </Trauma>
        foreach (var entry in fill.Contents)
        {
            var size = GetEntrySize(entry, getCount, protoMan, itemName, itemSystem); // Trauma - replace compFact with itemName

            if (entry.GroupId == null)
                totalSize += size;
            else
                groups[entry.GroupId] = Math.Max(size, groups.GetValueOrDefault(entry.GroupId));
        }

        return totalSize + groups.Values.Sum();
    }

    /// <summary>
    /// Tests that prototypes are not using multiple container fill components at the same time.
    /// </summary>
    [Test]
    public async Task NoMultipleContainerFillsTest()
    {
        var pair = Pair;
        // <Trauma> - microoptimisation
        var compFact = pair.Server.EntMan.ComponentFactory;
        var containerName = compFact.CompName<ContainerFillComponent>();
        var storageName = compFact.CompName<StorageFillComponent>();
        // </Trauma>

        Assert.Multiple(() =>
        {
            foreach (var (proto, fill) in pair.GetPrototypesWithComponent<EntityTableContainerFillComponent>())
            {
                // <Trauma> - use cached names instead of slop
                Assert.That(!proto.HasComp(storageName), $"Prototype {proto.ID} has both {nameof(EntityTableContainerFillComponent)} and {nameof(StorageFillComponent)}.");
                Assert.That(!proto.HasComp(containerName), $"Prototype {proto.ID} has both {nameof(EntityTableContainerFillComponent)} and {nameof(ContainerFillComponent)}.");
                // </Trauma>
            }

            foreach (var (proto, fill) in pair.GetPrototypesWithComponent<ContainerFillComponent>())
            {
                // <Trauma> - use cached names instead of slop
                Assert.That(!proto.HasComp(storageName), $"Prototype {proto.ID} has both {nameof(ContainerFillComponent)} and {nameof(StorageFillComponent)}.");
                // </Trauma>
            }
        });
    }
}
