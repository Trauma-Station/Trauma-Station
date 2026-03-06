// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Construction;
using Content.Trauma.Common.Knowledge.Components;
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

        if (_knowledge.GetContainer(ent) is not {} brain)
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

        var item = args.Entity;
        var quality = EnsureComp<QualityComponent>(item);
        // quality is affected by practical skills, something can be easy to understand but hard to pull off
        foreach (var (id, mastery) in (proto.Practical ?? proto.Theory))
        {
            quality.LevelDeltas[id] = mastery;
        }
        Dirty(item, quality);

        _quality.RollQuality((item, quality), ent);
    }
}
