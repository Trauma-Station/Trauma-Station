// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract class SharedInstantWorldTargetActionSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WorldTargetActionComponent, ValidateInstantWorldTargetActionEvent>(OnValidate);
    }

    private void OnValidate(Entity<WorldTargetActionComponent> ent, ref ValidateInstantWorldTargetActionEvent args)
    {
        if (ent.Comp.Event is not InstantWorldTargetActionEvent instantWorldEv)
        {
            args.Result = false;
            return;
        }

        instantWorldEv.Target = EntityCoordinates.Invalid;
        instantWorldEv.Entity = null;

        _adminLogger.Add(LogType.Action,
            $"{ToPrettyString(args.User):user} is performing the {Name(ent):action} action provided by {ToPrettyString(args.Provider):provider}.");

        args.Result = true;
    }
}
