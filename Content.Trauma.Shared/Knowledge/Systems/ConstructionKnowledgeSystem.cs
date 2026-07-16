// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Popups;
using Content.Trauma.Common.Construction;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Quality;
using Content.Trauma.Shared.Forging;
using Content.Trauma.Shared.Knowledge.Quality;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Controls construction knowledge requirements.
/// </summary>
public sealed partial class ConstructionKnowledgeSystem : EntitySystem
{
    [Dependency] private QualitySystem _quality = default!;
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<QualityPrototype> BaseQuality = "BaseQuality";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, ConstructAttemptEvent>(OnConstructAttempt);
        SubscribeLocalEvent<KnowledgeHolderComponent, ConstructedEvent>(OnConstructed);
        SubscribeLocalEvent<KnowledgeHolderComponent, ForgingCompletedEvent>(OnForgingCompleted);
    }

    private void OnConstructAttempt(Entity<KnowledgeHolderComponent> ent, ref ConstructAttemptEvent args)
    {
        if (args.Cancelled || !ProtoMan.Resolve<ConstructionPrototype>(args.Prototype, out var proto))
            return;

        if (_knowledge.GetContainer(ent) is not { } brain)
        {
            if (args.LogError)
                _popup.PopupEntity("You have no brain!", ent, ent, PopupType.MediumCaution);
            args.Cancelled = true;
            return;
        }

        // require theory knowledge mastery, you can't make something if you cant even understand what it is
        // practical knowledge just controls how easy it is to mess up
        foreach (var (id, mastery) in proto.Theory)
        {
            if (!brain.Comp.KnowledgeDict.TryGetValue(id, out var unit) || _knowledge.GetMastery(unit) < mastery)
            {
                if (args.LogError)
                {
                    var masteryName = _knowledge.GetMasteryString(mastery);
                    var name = ProtoMan.Index(id).Name;
                    _popup.PopupEntity($"You are missing {masteryName} {name} to construct that!", ent, ent, PopupType.MediumCaution);
                }
                args.Cancelled = true;
                return;
            }
        }
    }

    private void OnConstructed(Entity<KnowledgeHolderComponent> ent, ref ConstructedEvent args)
    {
        if (!ProtoMan.Resolve<ConstructionPrototype>(args.Prototype, out var proto))
            return;

        // TODO: grant xp when building shit

        // combines practical and theory knowledge together
        var levelDeltas = new Dictionary<EntProtoId, int>();
        if (proto.Practical is { } practical)
        {
            foreach (var (id, mastery) in practical)
            {
                levelDeltas[id] = mastery;
            }
        }
        foreach (var (id, mastery) in proto.Theory)
        {
            if (levelDeltas.TryGetValue(id, out var existing) && existing > mastery)
                continue;

            levelDeltas[id] = mastery;
        }

        // ignore quality code if the prototype doesn't want it
        if (!proto.UseQuality)
            return;

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

    private void OnForgingCompleted(Entity<KnowledgeHolderComponent> ent, ref ForgingCompletedEvent args)
    {
        // TODO: grant xp from forging
        var item = args.Target;
        if (EnsureComp<QualityComponent>(item, out var quality))
            return;

        // roll quality for fresh items
        var offset = args.Metal.MasteryOffset;
        foreach (var (id, mastery) in args.Item.Skills)
        {
            quality.LevelDeltas[id] = mastery + offset;
        }
        quality.QualityFactors = args.Item.QualityPrototype ?? BaseQuality;
        Dirty(item, quality);

        _quality.RollQuality((item, quality), ent);
    }
}
