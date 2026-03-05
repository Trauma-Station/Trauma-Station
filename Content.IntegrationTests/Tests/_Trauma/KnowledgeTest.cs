#nullable enable

using System.Collections.Generic;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared.Body;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
public sealed class KnowledgeTest
{
    public static readonly EntProtoId Human = "MobHuman";

    /// <summary>
    /// Makes sure that humans brains can go in and out.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task TestBrainKnowledgeTransfer()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var knowledge = entMan.System<SharedKnowledgeSystem>();
        var bodySystem = entMan.System<BodySystem>();

        await server.WaitPost(() =>
        {
            var coords = MapCoordinates.Nullspace;
            var human = entMan.SpawnEntity(Human, coords);

            Assert.That(entMan.HasComponent<KnowledgeHolderComponent>(human), "Human needs a KnowledgeHolder");
            var brain = knowledge.GetContainer(human);
            Assert.That(brain, Is.Not.Null, "Human has no knowledge container");
            var (uid, comp) = brain!.Value;
            Assert.That(uid != human, "Human's knowledge container was not the brain");
            Assert.That(comp.Holder, Is.EqualTo(human), "Brain's knowledge holder was not the human");

            Assert.That(bodySystem.RemoveOrgan(human, uid), "Failed to remove brain from the human");
            Assert.That(comp.Holder, Is.Null, "Brain's knowledge holder was not reset after removing it");
            Assert.That(knowledge.GetContainer(human), Is.Null, "Human's knowledge container was not reset after removing the brain");

            Assert.That(bodySystem.InsertOrgan(human, uid), "Failed to insert brain back into the human");
            Assert.That(comp.Holder, Is.EqualTo(human), "Brain's knowledge holder was not set after inserting it");
            Assert.That(knowledge.GetContainer(human)?.Owner, Is.EqualTo(uid), "Human's knowledge container was not set back to the brain after inserting it");

            entMan.DeleteEntity(human);
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Makes sure that mmis can go in and out of Borgs.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task TestBorgMMIKnowledgeTransfer()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var containerSys = entMan.System<SharedContainerSystem>();

        await server.WaitPost(() =>
        {
            var coords = MapCoordinates.Nullspace;

            var borg = entMan.SpawnEntity("PlayerBorgGeneric", coords);
            var mmi = entMan.SpawnEntity("MMI", coords);
            var brain = entMan.SpawnEntity("OrganHumanBrain", coords);

            var borgComp = entMan.GetComponent<KnowledgeHolderComponent>(borg);
            var brainSlot = containerSys.GetContainer(mmi, "brain_slot");
            containerSys.Insert(brain, brainSlot);

            var mmiSlot = containerSys.GetContainer(borg, "borg_brain");
            containerSys.Insert(mmi, mmiSlot);

            Assert.That(borgComp.KnowledgeEntity, Is.EqualTo(brain), "Borg should draw knowledge from the brain inside the MMI");

            containerSys.Remove(mmi, mmiSlot);

            Assert.That(borgComp.KnowledgeEntity, Is.Null, "Borg knowledge should clear after MMI ejection");
        });

        await pair.CleanReturnAsync();
    }


    /// <summary>
    /// Ensures that every Language Prototype has a corresponding knowledge entity.
    /// </summary>
    [Test]
    public async Task TestLanguageHasLanguageKnowledgeCounterpart()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var protoMan = server.ProtoMan;

        await server.WaitPost(() =>
        {
            var languages = protoMan.EnumeratePrototypes<LanguagePrototype>();
            var missingEntities = new List<string>();

            foreach (var lang in languages)
            {
                var expectedEntityId = $"Language{lang.ID}";

                if (!protoMan.HasIndex<EntityPrototype>(expectedEntityId))
                    missingEntities.Add($"{lang.ID} (Expected entity: {expectedEntityId})");
            }

            Assert.That(missingEntities, Is.Empty, $"The following languages are missing their 'Language<ID>' entity prototypes: \n{string.Join("\n", missingEntities)}");
        });

        await pair.CleanReturnAsync();
    }
}
