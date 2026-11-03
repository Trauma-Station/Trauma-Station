// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.GameTicking.Rules;
using Content.Goobstation.Shared.Roles;
using Content.Goobstation.Shared.Shadowling;
using Content.Goobstation.Shared.Shadowling.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Antag;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Zombies;
using Robust.Shared.Audio;

namespace Content.Goobstation.Server.Shadowling.Rules;

public sealed partial class ShadowlingRuleSystem : GameRuleSystem<ShadowlingRuleComponent>
{
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private NpcFactionSystem _npc = default!;
    [Dependency] private GameTicker _ticker = default!;

    private static readonly EntProtoId DeathSquad = "SpawnDeathsquad";
    private readonly SoundSpecifier _briefingSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Shadowling/shadowling.ogg");

    private readonly EntProtoId _mindRole = "MindRoleShadowling";

    private readonly ProtoId<NpcFactionPrototype> _shadowlingFactionId = "Shadowling";

    private readonly ProtoId<NpcFactionPrototype> _nanotrasenFactionId = "NanoTrasen";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowlingRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        SubscribeLocalEvent<ShadowlingRoleComponent, GetBriefingEvent>(OnGetBriefing);

        SubscribeLocalEvent<ShadowlingAscendEvent>(OnAscend);
        SubscribeLocalEvent<ShadowlingDeathEvent>(OnDeath);
    }

    private void OnDeath(ShadowlingDeathEvent args)
    {
        var rulesQuery = QueryActiveRules();
        while (rulesQuery.MoveNext(out _, out var rule, out _, out _))
        {
            var shadowlingCount = 0;
            var shadowlingDead = 0;
            var query = EntityQueryEnumerator<ShadowlingComponent>();

            while (query.MoveNext(out var uid, out _))
            {
                shadowlingCount++;
                if (_mob.IsDead(uid) || _mob.IsInvalidState(uid))
                    shadowlingDead++;
            }

            if (shadowlingCount == shadowlingDead)
                rule.WinCondition = ShadowlingWinCondition.Failure;
        }
    }

    private void OnAscend(ShadowlingAscendEvent args)
    {
        _ticker.StartGameRule(DeathSquad);
        var rulesQuery = QueryActiveRules();
        // TODO: only make the ascending one win bruh
        while (rulesQuery.MoveNext(out _, out var rule, out _, out _))
        {
            rule.WinCondition = ShadowlingWinCondition.Win;
            return;
        }
    }

    private void OnGetBriefing(EntityUid uid, ShadowlingRoleComponent component, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;
        var sling = HasComp<ShadowlingComponent>(ent);
        args.Briefing = Loc.GetString(sling ? "shadowling-briefing" : "thrall-briefing");
    }

    private void OnSelectAntag(EntityUid uid, ShadowlingRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        MakeShadowling(args.EntityUid);
    }

    public bool MakeShadowling(EntityUid target)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, _mindRole, mind, true);

        _npc.RemoveFaction(target, _nanotrasenFactionId, false);
        _npc.AddFaction(target, _shadowlingFactionId);

        var briefing = Loc.GetString("shadowling-role-greeting");

        _antag.SendBriefing(target, briefing, Color.MediumPurple, _briefingSound);

        EnsureComp<ZombieImmuneComponent>(target);
        EnsureComp<ShadowlingComponent>(target);
        return true;
    }

    protected override void AppendRoundEndText(Entity<ShadowlingRuleComponent> ent, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(ent, ref args);

        var winText = Loc.GetString($"shadowling-condition-{ent.Comp.WinCondition.ToString().ToLower()}");
        args.AddLine(winText);

        args.AddLine(Loc.GetString("shadowling-list-start"));

        var sessionData = _antag.GetAntagIdentifiers(ent.Owner);
        foreach (var (_, data, name) in sessionData)
        {
            var listing = Loc.GetString("shadowling-list-name", ("name", name), ("user", data.UserName));
            args.AddLine(listing);
        }
    }
}
