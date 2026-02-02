// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Shitmed.GameTicking.Rules.Components;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Shared._Shitcode.Roles;
using Content.Shared._Shitmed.Antags.Abductor;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;

    private static readonly EntProtoId DefaultAbductorVictimRule = "AbductorVictim";
    private static readonly EntProtoId MindRole = "MindRoleAbductorVictim";

    public void InitializeVictim()
    {
        SubscribeLocalEvent<AbductorComponent, SurgeryStepEvent>(OnSurgeryStepComplete);
    }

    private void OnSurgeryStepComplete(EntityUid uid, AbductorComponent comp, ref SurgeryStepEvent args)
    {
        if (!HasComp<SurgeryAddOrganStepComponent>(args.Step)
            || !args.Complete
            || HasComp<AbductorComponent>(args.Body) // no experimenting on yourself/your buddy
            || !TryComp<AbductorVictimComponent>(args.Body, out var victimComp) // you get nothing if you didn't gizmo
            || victimComp.Implanted // no farming
            || !HasComp<HumanoidAppearanceComponent>(args.Body) // experimenting on mice doesn't count
            || !_mind.TryGetMind(args.Body, out var mindId, out var mind) // stealing ssd doesn't count
            || !TryComp<ActorComponent>(args.Body, out var actor)
            || !HasComp<AbductorOrganComponent>(args.Tool))
            return;

        if (mindId == default
            || !_role.MindHasRole<AbductorVictimRoleComponent>(mindId, out _))
        {
            _role.MindAddRole(mindId, MindRole);
            victimComp.Implanted = true;
            _antag.ForceMakeAntag<AbductorVictimRuleComponent>(actor.PlayerSession, DefaultAbductorVictimRule);

            _adminLogManager.Add(LogType.Mind,
                LogImpact.Medium,
                $"{ToPrettyString(args.User)} has given {ToPrettyString(args.Body)} an abductee objective.");
        }
    }
}
