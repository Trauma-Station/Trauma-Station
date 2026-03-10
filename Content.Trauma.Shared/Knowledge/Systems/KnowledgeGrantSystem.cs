// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Handles granting knowledge through different components and ways.
/// </summary>
public sealed class KnowledgeGrantSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeGrantComponent, MapInitEvent>(OnKnowledgeGrantInit, after: [typeof(SharedKnowledgeSystem), typeof(InitialBodySystem)]);

        SubscribeLocalEvent<KnowledgeGrantOnUseComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<KnowledgeGrantOnUseComponent, KnowledgeLearnDoAfterEvent>(OnDoAfter);
    }

    private void OnKnowledgeGrantInit(Entity<KnowledgeGrantComponent> ent, ref MapInitEvent args)
    {
        _knowledge.AddKnowledgeUnits(ent.Owner, ent.Comp.Skills);
        RemComp(ent.Owner, ent.Comp);
    }

    private void StartLearningDoAfter(EntityUid user, Entity<KnowledgeGrantOnUseComponent> ent)
    {
        var args = new DoAfterArgs(EntityManager, user, ent.Comp.DoAfter, new KnowledgeLearnDoAfterEvent(), ent, ent, ent)
        {
            BreakOnDropItem = true,
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnUseInHand(Entity<KnowledgeGrantOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        StartLearningDoAfter(args.User, ent);
        args.Handled = true;
    }

    private void OnDoAfter(Entity<KnowledgeGrantOnUseComponent> ent, ref KnowledgeLearnDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || TerminatingOrDeleted(args.Target))
            return;

        DoAfter(ent, ref args);

        if (_net.IsClient)
        {
            // This forces the UI to update after learning if its open.
            var updateEv = new UpdateExperienceEvent();
            RaiseLocalEvent(args.User, ref updateEv);
        }
    }

    private void DoAfter(Entity<KnowledgeGrantOnUseComponent> ent, ref KnowledgeLearnDoAfterEvent args)
    {
        var user = args.User;
        if (!_timing.IsFirstTimePredicted ||
            args.Cancelled ||
            _knowledge.GetContainer(user) is not { } brain)
            return;

        args.Handled = true;

        if (ent.Comp.SingleUse)
        {
            // no checking if you already had it, don't waste a cqc book if you already know it chud
            foreach (var (id, level) in ent.Comp.Skills)
            {
                _knowledge.EnsureKnowledge(brain, id, level);
            }
            PredictedQueueDel(ent);
            PredictedSpawnNextToOrDrop(ent.Comp.Ash, user);
            return;
        }

        bool hasLearned = false;
        foreach (var (id, xp) in ent.Comp.Experience)
        {
            if (_knowledge.EnsureKnowledge(brain, id) is not { } skill)
                continue;

            if (!(!ent.Comp.Skills.TryGetValue(id, out var skillCap) || (skill.Comp.Level < skillCap || skillCap < 0)))
                continue;

            hasLearned = true;
            _knowledge.AddExperience(skill.AsNullable(), user, xp, skillCap);
        }

        args.Repeat = hasLearned;
        if (!hasLearned)
            _popup.PopupClient(Loc.GetString("knowledge-could-not-learn"), args.User, args.User, PopupType.SmallCaution);
    }
}

[Serializable, NetSerializable]
public sealed partial class KnowledgeLearnDoAfterEvent : SimpleDoAfterEvent;
