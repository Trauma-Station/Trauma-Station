// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Shitmed.Objectives.Components;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Objectives.Systems;

public sealed class ForceHereticObjectiveSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
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
