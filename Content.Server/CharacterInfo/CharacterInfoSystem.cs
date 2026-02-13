// <Trauma>
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;
// </Trauma>
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;

namespace Content.Server.CharacterInfo;

public sealed class CharacterInfoSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private readonly CommonKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    // </Trauma>
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestCharacterInfoEvent>(OnRequestCharacterInfoEvent);
    }

    private void OnRequestCharacterInfoEvent(RequestCharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue
            || args.SenderSession.AttachedEntity != GetEntity(msg.NetEntity))
            return;

        var entity = args.SenderSession.AttachedEntity.Value;

        var objectives = new Dictionary<string, List<ObjectiveInfo>>();
        var jobTitle = Loc.GetString("character-info-no-profession");
        string? briefing = null;
        if (_minds.TryGetMind(entity, out var mindId, out var mind))
        {
            // Get objectives
            foreach (var objective in mind.Objectives)
            {
                var info = _objectives.GetInfo(objective, mindId, mind);
                if (info == null)
                    continue;

                // group objectives by their issuer
                var issuer = Comp<ObjectiveComponent>(objective).LocIssuer;
                if (!objectives.ContainsKey(issuer))
                    objectives[issuer] = new List<ObjectiveInfo>();
                objectives[issuer].Add(info.Value);
            }

            if (_jobs.MindTryGetJobName(mindId, out var jobName))
                jobTitle = jobName;

            // Get briefing
            briefing = _roles.MindGetBriefing(mindId);
        }

        // <Trauma>
        var knowledge = new Dictionary<string, List<KnowledgeInfo>>();
        if (_knowledge.TryGetAllKnowledgeUnits(entity) is { } found)
        {
            foreach (var unit in found)
            {
                if (unit.Comp.Hidden)
                    continue;

                var (category, info) = _knowledge.GetKnowledgeInfo(unit);

                if (!knowledge.ContainsKey(category))
                    knowledge[category] = new List<KnowledgeInfo>();
                knowledge[category].Add(info);
            }
        }

        RaiseNetworkEvent(new CharacterInfoEvent(GetNetEntity(entity), jobTitle, objectives, briefing, knowledge), args.SenderSession);
        // </Trauma>
    }
}
