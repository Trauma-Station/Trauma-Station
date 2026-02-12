using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Shared.Knowledge.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Handles granting knowledge through different components and ways.
/// </summary>
public sealed class KnowledgeGrantSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeGrantComponent, MapInitEvent>(OnKnowledgeGrantInit, after: [typeof(SharedBodySystem)]);

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
        if (ent.Comp.DoAfter == null)
            return;

        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(ent.Comp.DoAfter.Value), new KnowledgeLearnDoAfterEvent(), ent, ent, ent)
        {
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnUseInHand(Entity<KnowledgeGrantOnUseComponent> ent, ref UseInHandEvent args)
    {
        var (uid, comp) = ent;

        if (comp.DoAfter is null)
        {
            _knowledge.AddKnowledgeUnits(args.User, ent.Comp.Experience);
        }
        else
        {
            StartLearningDoAfter(args.User, ent);
        }
    }

    private void OnDoAfter(Entity<KnowledgeGrantOnUseComponent> ent, ref KnowledgeLearnDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || TerminatingOrDeleted(args.Target))
            return;

        if (_netManager.IsClient)
            return;

        foreach (var skill in ent.Comp.Experience)
        {
            if (_knowledge.TryGetKnowledgeUnit(args.User, skill.Key) is not { } foundSkill)
            {
                _knowledge.TryAddKnowledgeUnit(args.User, new KeyValuePair<EntProtoId, int>(skill.Key, 0));
                continue;
            }

            if (TryComp<KnowledgeComponent>(foundSkill, out var foundComp) && (!ent.Comp.Skills.TryGetValue(skill.Key, out var skillCap) || (foundComp.Level < skillCap || skillCap < 0)))
            {
                var ev = new AddExperience(skill.Key, skill.Value);
                RaiseLocalEvent(args.User, ref ev);
                if (TryComp<LanguageKnowledgeComponent>(foundSkill, out var language))
                    _popup.PopupEntity(Loc.GetString("knowledge-learn-more", ("knowledge", Loc.GetString($"{language.LanguageId.Id}"))), args.User, args.User, PopupType.Small);
                else
                    _popup.PopupEntity(Loc.GetString("knowledge-learn-more", ("knowledge", Loc.GetString($"knowledge-{skill.Key.ToString()}"))), args.User, args.User, PopupType.Small);
            }
            else
            {
                if (TryComp<LanguageKnowledgeComponent>(foundSkill, out var language))
                    _popup.PopupEntity(Loc.GetString("knowledge-could-not-learn", ("knowledge", Loc.GetString($"{language.LanguageId.Id}"))), args.User, args.User, PopupType.Small);
                else
                    _popup.PopupEntity(Loc.GetString("knowledge-could-not-learn", ("knowledge", Loc.GetString($"knowledge-{skill.Key.ToString()}"))), args.User, args.User, PopupType.Small);
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
