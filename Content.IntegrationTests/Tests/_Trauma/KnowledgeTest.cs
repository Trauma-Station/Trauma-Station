// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Shared.Body;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Language;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class KnowledgeTest : GameTest
{
    private static readonly EntProtoId Borg = "PlayerBorgGeneric";
    private static readonly EntProtoId Brain = "OrganHumanBrain";
    private static readonly EntProtoId Human = "MobHuman";
    private static readonly EntProtoId MMI = "MMI";

    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private SharedContainerSystem _container = default!;
    [SidedDependency(Side.Server)] private SharedKnowledgeSystem _knowledge = default!;

    /// <summary>
    /// Makes sure that humans brains can go in and out.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task TestBrainKnowledgeTransfer()
    {
        await Server.WaitPost(() =>
        {
            var human = SSpawn(Human);

            Assert.That(SHasComp<KnowledgeHolderComponent>(human), "Human needs a KnowledgeHolder");
            var brain = _knowledge.GetContainer(human);
            Assert.That(brain, Is.Not.Null, "Human has no knowledge container");
            var (uid, comp) = brain!.Value;
            Assert.That(uid != human, "Human's knowledge container was not the brain");
            Assert.That(comp.Holder, Is.EqualTo(human), "Brain's knowledge holder was not the human");

            Assert.That(_body.RemoveOrgan(human, uid), "Failed to remove brain from the human");
            Assert.That(comp.Holder, Is.Null, "Brain's knowledge holder was not reset after removing it");
            Assert.That(_knowledge.GetContainer(human), Is.Null, "Human's knowledge container was not reset after removing the brain");

            Assert.That(_body.InsertOrgan(human, uid), "Failed to insert brain back into the human");
            Assert.That(comp.Holder, Is.EqualTo(human), "Brain's knowledge holder was not set after inserting it");
            Assert.That(_knowledge.GetContainer(human)?.Owner, Is.EqualTo(uid), "Human's knowledge container was not set back to the brain after inserting it");

            SDel(human);
        });
    }

    /// <summary>
    /// Makes sure that mmis can go in and out of Borgs.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task TestBorgMMIKnowledgeTransfer()
    {
        await Server.WaitPost(() =>
        {
            var borg = SSpawn(Borg);
            var mmi = SSpawn(MMI);
            var brain = SSpawn(Brain);

            var borgComp = SComp<KnowledgeHolderComponent>(borg);
            var brainSlot = _container.GetContainer(mmi, "brain_slot");
            _container.Insert(brain, brainSlot);

            var mmiSlot = _container.GetContainer(borg, "borg_brain");
            _container.Insert(mmi, mmiSlot);

            Assert.That(borgComp.KnowledgeEntity, Is.EqualTo(brain), "Borg should draw knowledge from the brain inside the MMI");

            _container.Remove(mmi, mmiSlot);

            Assert.That(borgComp.KnowledgeEntity, Is.Null, "Borg knowledge should clear after MMI ejection");
        });
    }


    /// <summary>
    /// Ensures that every Language Prototype has a corresponding knowledge entity.
    /// </summary>
    [Test]
    public async Task TestAllLanguageKnowledgeExists()
    {
        await Server.WaitAssertion(() =>
        {
            var missing = new List<string>();
            foreach (var lang in SProtoMan.EnumeratePrototypes<LanguagePrototype>())
            {
                var expectedEntityId = $"Language{lang.ID}";

                if (!SProtoMan.HasIndex<EntityPrototype>(expectedEntityId))
                    missing.Add($"- {lang.ID} (Expected entity: {expectedEntityId})");
            }

            Assert.That(missing, Is.Empty, $"The following languages are missing their 'Language<ID>' entity prototypes: \n{string.Join("\n", missing)}");
        });
    }
}
