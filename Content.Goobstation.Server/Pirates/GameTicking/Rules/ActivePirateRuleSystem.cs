// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Pirates;
using Content.Goobstation.Shared.Roles;
using Content.Server.Mind;
using Content.Shared.Antag;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Robust.Shared.Audio;

namespace Content.Goobstation.Server.Pirates.GameTicking.Rules;

public sealed partial class ActivePirateRuleSystem : GameRuleSystem<ActivePirateRuleComponent>
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;

    private static readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/Ambience/Antag/pirate_start.ogg");
    private static readonly EntProtoId MindRole = "MindRolePirate";
    private static readonly ProtoId<NpcFactionPrototype> PirateFaction = "PirateFaction";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivePirateRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<PirateRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnAntagSelect(Entity<ActivePirateRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (_mind.TryGetMind(args.EntityUid, out var mindId, out var mind) && TryMakePirate(args.EntityUid))
            ent.Comp.Pirates.Add((mindId, mind));
    }

    private void OnGetBriefing(Entity<PirateRoleComponent> ent, ref GetBriefingEvent args)
    {
        var briefingShort = Loc.GetString("antag-pirate-briefing-short");
        args.Briefing = briefingShort;
    }

    protected override void AppendRoundEndText(Entity<ActivePirateRuleComponent> ent, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(ent, ref args);

        var (uid, comp) = ent;
        if (comp.BoundSiphon != null &&
            TryComp<ResourceSiphonComponent>(comp.BoundSiphon, out var siphon) &&
            siphon.Active)
        {
            args.AddLine(Loc.GetString("pirate-roundend-append-siphon", ("num", siphon.Credits)));
        }

        args.AddLine(Loc.GetString("pirate-roundend-append", ("num", comp.Credits)));

        args.AddLine($"\n{Loc.GetString("pirate-roundend-list")}");
        var antags = _antag.GetAntagIdentifiers(uid);
        foreach (var (_, sessionData, name) in antags)
        {
            // nukies? in my pirate rule? how queer...
            args.AddLine(Loc.GetString("nukeops-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
    }

    public bool TryMakePirate(EntityUid target)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, MindRole.Id, mind, true);

        var briefing = Loc.GetString("antag-pirate-briefing");
        _antag.SendBriefing(target, briefing, Color.OrangeRed, BriefingSound);

        _npcFaction.AddFaction(target, PirateFaction); // yaml fucking sucks!!!

        return true;
    }
}
