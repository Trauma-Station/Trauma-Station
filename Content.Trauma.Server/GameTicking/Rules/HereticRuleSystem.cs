// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Content.Trauma.Server.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Server.Objectives.Components;
using Content.Trauma.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Timing;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed partial class HereticRuleSystem : GameRuleSystem<HereticRuleComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private ObjectivesSystem _objective = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private GameTicker _ticker = default!;

    public static readonly SoundSpecifier BriefingSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/heretic_gain.ogg");

    public static readonly SoundSpecifier BriefingSoundIntense =
        new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/heretic_gain_intense.ogg");

    public static EntProtoId MindRole = "MindRoleHeretic";

    public static EntProtoId RealityShift = "EldritchInfluence";

    protected override void Started(EntityUid uid, HereticRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextPassivePointUpdate = _timing.CurTime + component.PassivePointCooldown;
    }

    protected override void ActiveTick(EntityUid uid, HereticRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var now = _timing.CurTime;

        if (now < component.NextPassivePointUpdate)
            return;

        component.NextPassivePointUpdate = now + component.PassivePointCooldown;

        foreach (var mind in component.Minds)
        {
            _heretic.UpdateMindKnowledge(mind, null, SharedHereticSystem.OneKnowledgePoint);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetBriefing(Entity<HereticRoleComponent> ent, ref GetBriefingEvent args)
    {
        var uid = args.Mind.Comp.OwnedEntity;

        if (uid == null)
            return;

        var briefingShort = Loc.GetString("heretic-role-greeting-short");
        args.Append(briefingShort);
    }

    [SubscribeLocalEvent]
    private void OnAntagSelect(Entity<HereticRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        TryMakeHeretic(args.EntityUid, ent.Comp);

        SpawnInfluence(ent.Comp.RealityShiftPerHeretic);
    }

    public void SpawnInfluence(int amount)
    {
        if (amount <= 0)
            return;

        if (!TryGetRandomStation(out var station))
            return;

        if (GetStationMainGrid((station.Value, Comp<StationDataComponent>(station.Value))) is not { } grid)
            return;

        for (var i = 0; i < amount; i++)
        {
            if (TryFindTileOnGrid(grid, out _, out var coords))
                Spawn(RealityShift, coords);
        }
    }

    public bool TryMakeHeretic(EntityUid target, HereticRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, MindRole.Id, mind, true);

        // briefing
        if (HasComp<MetaDataComponent>(target))
        {
            _antag.SendBriefing(target, Loc.GetString("heretic-role-greeting-fluff"), Color.MediumPurple, null);
            _antag.SendBriefing(target, Loc.GetString("heretic-role-greeting"), Color.Red, BriefingSound);
        }

        // heretic after role because it requires store on startup
        EnsureComp<HereticComponent>(mindId);

        rule.Minds.Add(mindId);

        _ui.SetUi(mindId, HereticLivingHeartKey.Key, new InterfaceData("LivingHeartMenuBoundUserInterface", -1));

        return true;
    }

    [SubscribeLocalEvent]
    private void OnTextPrepend(Entity<HereticRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var sb = new StringBuilder();

        var mostKnowledge = 0f;
        var mostKnowledgeName = string.Empty;

        var query = EntityQueryEnumerator<HereticComponent, MindComponent>();
        while (query.MoveNext(out var mindId, out var heretic, out var mind))
        {
            var name = _objective.GetTitle((mindId, mind), Name(mind.OwnedEntity ?? mindId));
            if (_mind.TryGetObjectiveComp<HereticKnowledgeConditionComponent>(mindId, out var objective, mind))
            {
                if (objective.Researched <= mostKnowledge)
                    continue;

                mostKnowledge = objective.Researched;
                mostKnowledgeName = name;
            }

            var message =
                $"roundend-prepend-heretic-ascension-{(heretic.Ascended ? "success" : heretic.CanAscend ? "fail" : "fail-owls")}";
            var str = Loc.GetString(message, ("name", name));
            sb.AppendLine(str);
        }

        sb.AppendLine("\n" + Loc.GetString("roundend-prepend-heretic-knowledge-named",
            ("name", mostKnowledgeName),
            ("number", mostKnowledge)));

        args.Text = sb.ToString();
    }

    public void SpawnERTOnAscension()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var rule, out _))
        {
            if (rule.HasAHereticAscended)
                continue;

            rule.HasAHereticAscended = true;
            _ticker.StartGameRule(rule.ERTEvent);
            break;
        }
    }
}
