// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Server.Objectives.Components;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Database;
using Content.Shared.Objectives.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Medical.Server.Objectives.Systems;

public sealed class ForceHereticObjectiveSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public static readonly EntProtoId HereticRule = "Heretic";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForceHereticObjectiveComponent, ObjectiveAfterAssignEvent>(OnAssigned);
    }

    private void OnAssigned(Entity<ForceHereticObjectiveComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (args.Mind.CurrentEntity is not {} uid ||
            !TryComp<ActorComponent>(uid, out var actor))
            return;

        _antag.ForceMakeAntag<HereticRuleComponent>(actor.PlayerSession, HereticRule);

        _adminLog.Add(LogType.Mind,
            LogImpact.High,
            $"{ToPrettyString(uid)} has been given heretic status by objective {ToPrettyString(ent)}");
    }
}
