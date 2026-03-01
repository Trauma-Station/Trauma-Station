// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._EinsteinEngines.Language.Components;
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
        StartLearningDoAfter(args.User, ent);
    }

    private void OnDoAfter(Entity<KnowledgeGrantOnUseComponent> ent, ref KnowledgeLearnDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || TerminatingOrDeleted(args.Target))
            return;

        DoAfter(ent, ref args);

        if (_net.IsClient)
        {
            // This forces the UI to update after learning if its open.
            var evNetUpdate = new UpdateExperienceEvent();
            RaiseLocalEvent(args.User, ref evNetUpdate);
        }
    }

    private void DoAfter(Entity<KnowledgeGrantOnUseComponent> ent, ref KnowledgeLearnDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var skill in ent.Comp.Experience)
        {
            if (_knowledge.TryGetKnowledgeUnit(args.User, skill.Key) is not { } foundSkill)
            {
                _knowledge.TryAddKnowledgeUnit(args.User, (skill.Key, 0));
                continue;
            }

            if (TryComp<KnowledgeComponent>(foundSkill, out var foundComp) && (!ent.Comp.Skills.TryGetValue(skill.Key, out var skillCap) || (foundComp.Level < skillCap || skillCap < 0)))
            {
                var ev = new AddExperienceEvent(skill.Key, skill.Value);
                RaiseLocalEvent(args.User, ref ev);
            }
            else
            {
                _popup.PopupClient(Loc.GetString("knowledge-could-not-learn", ("knowledge", _knowledge.KnowledgeString(foundSkill))), args.User, args.User, PopupType.Small);
            }
        }
        args.Handled = true;

        bool canStillLearn = false;
        foreach (var skill in ent.Comp.Experience)
        {
            if (_knowledge.TryGetKnowledgeUnit(args.User, skill.Key) is { } foundSkill && TryComp<KnowledgeComponent>(foundSkill, out var foundComp) && (!ent.Comp.Skills.TryGetValue(skill.Key, out var skillCap) || (foundComp.Level < skillCap || skillCap < 0)))
            {
                canStillLearn = true;
                break;
            }
        }

        if (canStillLearn)
            StartLearningDoAfter(args.User, ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class KnowledgeLearnDoAfterEvent : SimpleDoAfterEvent;
