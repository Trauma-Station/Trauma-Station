// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Construction;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Quality;
using Content.Trauma.Shared.Knowledge.Quality;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Controls construction knowledge requirements.
/// </summary>
public sealed class ConstructionKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly QualitySystem _quality = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    private static readonly ProtoId<QualityPrototype> BaseQuality = "BaseQuality";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, ConstructAttemptEvent>(OnConstructAttempt);
        SubscribeLocalEvent<KnowledgeHolderComponent, ConstructedEvent>(OnConstructed);
    }

    private void OnConstructAttempt(Entity<KnowledgeHolderComponent> ent, ref ConstructAttemptEvent args)
    {
        if (args.Cancelled || !_proto.Resolve<ConstructionPrototype>(args.Prototype, out var proto))
            return;

        if (_knowledge.GetContainer(ent) is not { } brain)
        {
            if (args.LogError)
                Log.Error($"{ToPrettyString(ent)} tried to construct {args.Prototype} without having a knowledge container!");
            args.Cancelled = true;
            return;
        }

        // require theory knowledge mastery, you can't make something if you cant even understand what it is
        // practical knowledge just controls how easy it is to mess up
        foreach (var (id, mastery) in proto.Theory)
        {
            if (!brain.Comp.KnowledgeDict.TryGetValue(id, out var unit) || _knowledge.GetMastery(unit) < mastery)
            {
                Log.Error($"{ToPrettyString(ent)} tried to construct {args.Prototype} but is missing {id} mastery {mastery}!");
                args.Cancelled = true;
                return;
            }
        }
    }

    private void OnConstructed(Entity<KnowledgeHolderComponent> ent, ref ConstructedEvent args)
    {
        if (!_proto.Resolve<ConstructionPrototype>(args.Prototype, out var proto))
            return;

        // TODO: grant xp when building shit

        // combines practical and theory knowledge together
        var levelDeltas = new Dictionary<EntProtoId, int>();
        if (proto.Practical is { })
        {
            foreach (var (id, mastery) in (proto.Practical))
            {
                levelDeltas[id] = mastery;
            }
        }
        foreach (var (id, mastery) in (proto.Theory))
        {
            if (levelDeltas.ContainsKey(id) && levelDeltas[id] > mastery)
                continue;

            levelDeltas[id] = mastery;
        }

        // ignore quality code if the prototype doesn't want it
        if (!proto.UseQuality)
        {
            // Grants experience to the user even if the item doesn't get a quality.
            if (_knowledge.GetContainer(ent) is not { } brain)
                return;

            var (knowledgeToUse, lowestId, _, skillDelta) = _quality.FindLowestDelta(brain, levelDeltas);

            _knowledge.AddExperience(brain, knowledgeToUse, 3, _knowledge.GetInverseMastery(skillDelta + 2));

            if (lowestId is not { } actualId)
                return;

            _knowledge.AddExperience(brain, actualId, 3, _knowledge.GetInverseMastery(skillDelta + 2));
            return;
        }

        var item = args.Entity;
        var quality = EnsureComp<QualityComponent>(item);
        // quality is affected by practical skills, something can be easy to understand but hard to pull off
        foreach (var (id, mastery) in levelDeltas)
        {
            quality.LevelDeltas[id] = mastery;
        }
        quality.QualityFactors = proto.QualityPrototype ?? BaseQuality;
        Dirty(item, quality);

        _quality.RollQuality((item, quality), ent);
    }
}
