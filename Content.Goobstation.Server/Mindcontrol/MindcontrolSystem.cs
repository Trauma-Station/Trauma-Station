// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Mindcontrol;
using Content.Goobstation.Shared.Roles;
using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Stunnable;
using Content.Shared.Antag;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Popups;
using Content.Trauma.Common.Mindshield;
using Robust.Server.Player;

namespace Content.Goobstation.Server.Mindcontrol;

public sealed partial class MindcontrolSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogManager = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private StunSystem _stun = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPlayerManager _player = default!;

    private static EntProtoId MindRole = "MindRoleBrainwashed";

    [SubscribeLocalEvent]
    public void OnStartup(EntityUid uid, MindcontrolledComponent component, ComponentStartup arg)
    {
        _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(5f)); //dont need this but, but its a still a good indicator from how Revolution and subverted silicon does it
    }

    [SubscribeLocalEvent]
    public void OnShutdown(EntityUid uid, MindcontrolledComponent component, ComponentShutdown arg)
    {
        if (TerminatingOrDeleted(uid))
            return;

        _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(5f));
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _role.MindRemoveRole<MindcontrolledRoleComponent>((mindId, mind));
        _popup.PopupEntity(Loc.GetString("mindcontrol-popup-stop"), uid, PopupType.Large);
        _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} is no longer Mindcontrolled.");
    }

    public void Start(EntityUid uid, MindcontrolledComponent component)
    {
        if (component.Master is not {} master ||
            _mindShield.IsShielded(uid) || // you somehow managed to implant someone with a mindshield.
            uid == master || // good job, you implanted yourself
            !_mind.TryGetMind(uid, out var mindId, out var mind)) // no mind, how can you mindcontrol with no mind?
            return;

        _role.MindAddRole(mindId, MindRole, mind, silent: true);

        if (_role.MindHasRole<MindcontrolledRoleComponent>((mindId, mind), out var mr))
            AddComp(mr.Value, new RoleBriefingComponent { Briefing = MakeBriefing(master) }, true);

        if (_player.TryGetSessionById(mind.UserId, out var session) &&
            session != null &&
            !component.BriefingSent)
        {
            _popup.PopupEntity(Loc.GetString("mindcontrol-popup-start"), uid, PopupType.LargeCaution);
            _antag.SendBriefing(session, Loc.GetString("mindcontrol-briefing-start", ("master", Name(master))), Color.Red, component.MindcontrolStartSound);
            component.BriefingSent = true;
        }
        _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} is Mindcontrolled by {ToPrettyString(master)}.");
    }

    // OnMindAdded is if somone without a mind gets implanted, like Ian before given cognizine or someone dead ghost.
    [SubscribeLocalEvent]
    private void OnMindAdded(EntityUid uid, MindcontrolledComponent component, MindAddedMessage args)
    {
        if (!_role.MindHasRole<MindcontrolledRoleComponent>(args.Mind.Owner))
            Start(uid, component); //goes agein if comp added before mind.
    }

    [SubscribeLocalEvent]
    private void OnMindShielded(Entity<MindcontrolledComponent> ent, ref MindShieldedEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnMindRemoved(EntityUid uid, MindcontrolledComponent component, MindRemovedMessage args)
    {
        _role.MindRemoveRole<MindcontrolledRoleComponent>(args.Mind.Owner);
    }

    [SubscribeLocalEvent]
    private void OnGetBriefing(Entity<MindcontrolledRoleComponent> target, ref GetBriefingEvent args)
    {
        if (!TryComp<MindComponent>(target.Owner, out var mind) || mind.OwnedEntity == null)
            return;

        args.Append(MakeBriefing(target.Comp.MasterUid));
    }

    private string MakeBriefing(EntityUid? masterId)
    {
        var briefing = Loc.GetString("mindcontrol-briefing-get");
        if (masterId != null) // Returns null if Master is gibbed
        {
            TryComp<MetaDataComponent>(masterId, out var metadata);
            if (metadata != null)
                briefing += "\n " + Loc.GetString("mindcontrol-briefing-get-master", ("master", metadata.EntityName)) + "\n";
        }
        return briefing;
    }
}
