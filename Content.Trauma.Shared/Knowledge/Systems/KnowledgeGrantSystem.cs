using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Shared.Knowledge.Components;
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

    private void OnUseInHand(Entity<KnowledgeGrantOnUseComponent> ent, ref UseInHandEvent args)
    {
        var (uid, comp) = ent;

        if (comp.DoAfter is null)
        {
            _knowledge.AddKnowledgeUnits(args.User, ent.Comp.Experience);
        }
        else
        {
            var doAfter = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(comp.DoAfter.Value), new KnowledgeLearnDoAfterEvent(), uid, uid, uid)
            {
                BreakOnDropItem = true,
                BreakOnHandChange = true,
                BreakOnDamage = true,
                BreakOnMove = true,
                BlockDuplicate = true,
            };

            _doAfter.TryStartDoAfter(doAfter);
        }
    }

    private void OnDoAfter(Entity<KnowledgeGrantOnUseComponent> ent, ref KnowledgeLearnDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || TerminatingOrDeleted(args.Target))
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
                if (TryComp<LanguageKnowledgeComponent>(foundSkill, out _))
                    _popup.PopupEntity(Loc.GetString("knowledge-learn-more", ("knowledge", Loc.GetString($"{skill.Key.ToString()}"))), args.User, args.User, PopupType.Medium);
                else
                    _popup.PopupEntity(Loc.GetString("knowledge-learn-more", ("knowledge", Loc.GetString($"knowledge-{skill.Key.ToString()}"))), args.User, args.User, PopupType.Medium);
            }
            else
            {
                if (TryComp<LanguageKnowledgeComponent>(foundSkill, out _))
                    _popup.PopupEntity(Loc.GetString("knowledge-could-not-learn", ("knowledge", Loc.GetString($"{skill.Key.ToString()}"))), args.User, args.User, PopupType.Medium);
                else
                    _popup.PopupEntity(Loc.GetString("knowledge-could-not-learn", ("knowledge", Loc.GetString($"knowledge-{skill.Key.ToString()}"))), args.User, args.User, PopupType.Medium);
            }
        }
        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class KnowledgeLearnDoAfterEvent : SimpleDoAfterEvent;
