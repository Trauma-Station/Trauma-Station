// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Handles granting knowledge through different components and ways.
/// </summary>
public abstract partial class SharedKnowledgeGrantSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeGrantComponent, MapInitEvent>(OnKnowledgeGrantInit, after: [typeof(SharedKnowledgeSystem), typeof(InitialBodySystem)]);

        SubscribeLocalEvent<KnowledgeGrantOnUseComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<KnowledgeGrantOnUseComponent, GymRepPerformedMessage>(OnUiMessage);
    }

    private void OnKnowledgeGrantInit(Entity<KnowledgeGrantComponent> ent, ref MapInitEvent args)
    {
        _knowledge.AddKnowledgeUnits(ent.Owner, ent.Comp.Skills);
        RemComp(ent.Owner, ent.Comp);
    }

    private void OnUseInHand(Entity<KnowledgeGrantOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (ent.Comp.Instant)
        {
            if (_knowledge.GetContainer(args.User) is not { } brain)
                return;

            // no checking if you already had it, don't waste a cqc book if you already know it chud
            foreach (var (id, level) in ent.Comp.Knowledge)
            {
                _knowledge.EnsureKnowledge(brain, id, level);
            }
            if (ent.Comp.GrantEverything)
            {
                foreach (var id in _knowledge.AllSkills.Keys)
                {
                    _knowledge.EnsureKnowledge(brain, id, 100);
                }
            }
            if (ent.Comp.SingleUse)
            {
                PredictedQueueDel(ent);
                PredictedSpawnNextToOrDrop(ent.Comp.Ash, args.User);
            }
            return;
        }

        if (!_ui.TryGetOpenUi(ent.Owner, GymUiKey.Key, out var activeGymWindow))
        {
            _ui.OpenUi(ent.Owner, GymUiKey.Key, args.User, true);
            return;
        }
        OnActivate(ent, args.User, activeGymWindow);
    }

    protected abstract void OnActivate(Entity<KnowledgeGrantOnUseComponent> ent, EntityUid user, BoundUserInterface window);

    private void OnUiMessage(Entity<KnowledgeGrantOnUseComponent> ent, ref GymRepPerformedMessage args)
    {
        HandleRep(ent, args.Actor, args.TimingAccuracy);
    }

    protected void HandleRep(Entity<KnowledgeGrantOnUseComponent> ent, EntityUid actor, float timingAccuracy)
    {
        _stamina.TakeStaminaDamage(actor, Math.Max(1 - timingAccuracy, 0.0f) * 15);
        if (timingAccuracy < 0.4f)
        {
            _popup.PopupClient("Poor form!", actor, actor, PopupType.SmallCaution);
            return;
        }

        if (_knowledge.GetContainer(actor) is not { } brain)
            return;

        bool hasLearned = false;
        foreach (var (id, xp) in ent.Comp.Experience)
        {
            if (_knowledge.EnsureKnowledge(brain, id) is not { } skill)
                continue;

            if (!(!ent.Comp.Knowledge.TryGetValue(id, out var skillCap) || (_knowledge.GetLevel(skill) < skillCap || skillCap < 0)))
                continue;

            hasLearned = true;
            _knowledge.AddExperience(skill, actor, xp, skillCap);
        }

        if (!hasLearned)
            _popup.PopupClient(Loc.GetString("knowledge-could-not-learn"), actor, actor, PopupType.SmallCaution);
        else
        {
            var qualityString = timingAccuracy > 0.85f ? "Perfect!" : "Good!";
            _popup.PopupClient($"{qualityString}", actor, actor, PopupType.Medium);
        }
    }
}
