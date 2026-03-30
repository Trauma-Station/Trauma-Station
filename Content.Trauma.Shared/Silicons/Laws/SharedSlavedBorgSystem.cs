// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emoting;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Silicons.Borgs;
using Content.Trauma.Common.Silicons.Laws;
using Content.Trauma.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Silicons.Laws;

public abstract class SharedSlavedBorgSystem : CommonSlavedBorgSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedBorgSystem _borg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiRemoteBrainComponent, BorgChassisInteractAfterEvent>(HandleChassisInteraction);
        SubscribeLocalEvent<AiRemoteBrainComponent, BorgBrainRemovedEvent>(HandleRemoteBrainRemoved);
    }

    private void HandleChassisInteraction(Entity<AiRemoteBrainComponent> ent, ref BorgChassisInteractAfterEvent args)
    {
        if (!TryComp<BorgChassisComponent>(args.Chassis, out var chassis) || chassis.BrainEntity is { } || !_whitelist.IsWhitelistPassOrNull(chassis.BrainWhitelist, ent.Owner))
            return;

        EnsureComp<AiRemoteControllerComponent>(args.Chassis);
        _container.Insert(ent.Owner, chassis.BrainContainer);
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User):player} installed ai remote brain {ToPrettyString(ent.Owner)} into borg {ToPrettyString(args.Chassis)}");
        _borg.TryActivate((args.Chassis, chassis));
        args.Handled = true;
    }

    private void HandleRemoteBrainRemoved(Entity<AiRemoteBrainComponent> ent, ref BorgBrainRemovedEvent args)
    {
        if (!TryComp<BorgChassisComponent>(args.Chassis, out var chassis))
            return;

        _borg.BorgDeactivate((args.Chassis, chassis), user: args.Chassis);
        RemComp<AiRemoteControllerComponent>(args.Chassis);
        RemComp<StationAiVisionComponent>(args.Chassis);
    }

    public override bool IsSlavedBorg(EntityUid uid)
    {
        return HasComp<AiRemoteControllerComponent>(uid);
    }
}
