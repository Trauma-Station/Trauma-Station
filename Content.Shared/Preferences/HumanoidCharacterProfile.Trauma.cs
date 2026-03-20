using Content.Goobstation.Common.Barks;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Preferences;

/// <summary>
/// Trauma - settings for barks and skills
/// </summary>
public sealed partial class HumanoidCharacterProfile
{
    [DataField]
    public ProtoId<BarkPrototype> BarkVoice = HumanoidProfileSystem.DefaultBarkVoice;

    /// <summary>
    /// Changes to mastery level of every skill for this character, added to the species masteries.
    /// </summary>
    [DataField]
    public KnowledgeProfile Knowledge = new();

    public static ProtoId<BarkPrototype> RandomBark(IRobustRandom random, IPrototypeManager proto, string species)
    {
        var barks = new List<ProtoId<BarkPrototype>>();
        foreach (var bark in proto.EnumeratePrototypes<BarkPrototype>())
        {
            if (bark.RoundStart && bark.SpeciesWhitelist?.Contains(species) != false)
                barks.Add(bark.ID);
        }

        return random.Pick(barks);
    }

    public HumanoidCharacterProfile WithBarkVoice(ProtoId<BarkPrototype> barkVoice)
    {
        return new(this) { BarkVoice = barkVoice };
    }

    public HumanoidCharacterProfile WithKnowledge(KnowledgeProfile knowledge)
    {
        return new(this) { Knowledge = knowledge };
    }

    private void EnsureValidTrauma(IDependencyCollection collection, IPrototypeManager proto)
    {
        if (!proto.HasIndex(BarkVoice))
            BarkVoice = HumanoidProfileSystem.DefaultBarkVoice;

        var entMan = collection.Resolve<IEntityManager>();
        var knowledge = entMan.System<CommonKnowledgeSystem>();
        var parent = proto.Index(Species).Knowledge;
        knowledge.EnsureProfileValid(parent, ref Knowledge);
    }
}
