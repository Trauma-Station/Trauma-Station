using Content.Medical.Shared.Augments;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Station.Systems;
using Content.Shared.Body;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
public sealed class KnowledgeTest
{
    /// <summary>
    /// Makes sure that engineers can build an APC.
    /// </summary>
    /// <returns></returns>


    /// <summary>
    /// Makes sure that humans brains can go in and out.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task TestBrainKnowledgeTransfer()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var containerSys = entMan.System<SharedContainerSystem>();
        var bodySystem = entMan.System<BodySystem>();

        await server.WaitPost(() =>
        {
            var coords = MapCoordinates.Nullspace;
            var human = entMan.SpawnEntity("MobHuman", coords);

            Assert.That(entMan.HasComponent<KnowledgeHolderComponent>(human), "Human needs a KnowledgeHolder");
            var humanComp = entMan.GetComponent<KnowledgeHolderComponent>(human);

            EntityUid? brain = null;
            BaseContainer? brainSlot = null;
            if (bodySystem.GetOrgans(human) is { } organs)
            {
                foreach (var organ in organs)
                {
                    if (entMan.HasComponent<KnowledgeContainerComponent>(organ))
                    {
                        brain = organ;
                        if (entMan.TryGetComponent<TransformComponent>(organ, out var transform))
                        containerSys.TryGetContainingContainer((organ, transform), out brainSlot);
                        break;
                    }
                }
            }

            Assert.That(brain, Is.Not.Null, "Human should spawn with a brain inside");

            Assert.That(humanComp.KnowledgeEntity, Is.EqualTo(brain), "Human should be linked to its internal brain on spawn");

            containerSys.Remove(brain.Value, brainSlot);
            Assert.That(humanComp.KnowledgeEntity, Is.Null, "KnowledgeEntity should be null after brain removal");

            containerSys.Insert(brain.Value, brainSlot);
            Assert.That(humanComp.KnowledgeEntity, Is.EqualTo(brain), "KnowledgeEntity should re-link after brain is re-inserted");
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
            var brain = entMan.SpawnEntity("OrganBrain", coords);

            var borgComp = entMan.GetComponent<KnowledgeHolderComponent>(borg);
            var brainSlot = containerSys.EnsureContainer<ContainerSlot>(mmi, "brain");
            containerSys.Insert(brain, brainSlot);

            var mmiSlot = containerSys.EnsureContainer<ContainerSlot>(borg, "mmi_slot");
            containerSys.Insert(mmi, mmiSlot);

            Assert.That(borgComp.KnowledgeEntity, Is.EqualTo(brain), "Borg should draw knowledge from the brain inside the MMI");

            containerSys.Remove(mmi, mmiSlot);

            Assert.That(borgComp.KnowledgeEntity, Is.Null, "Borg knowledge should clear after MMI ejection");
        });

        await pair.CleanReturnAsync();
    }
}
