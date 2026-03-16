using Content.Shared.Actions.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Heretic;
using Content.Shitcode.Common.Actions;
using Robust.Shared.Map;

namespace Content.Shitcode.Shared.Actions;

public sealed partial class ShitcodeSharedActionsSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WorldTargetActionComponent, CheckWorldInstantActionEvent>(Fallback);
    }

    private void Fallback(Entity<WorldTargetActionComponent> ent, ref CheckWorldInstantActionEvent args)
    {
        var user = args.User;
        var provider = args.Provider;
        if (ent.Comp.Event is not InstantWorldTargetActionEvent instantWorldEv)
            return;

        instantWorldEv.Target = EntityCoordinates.Invalid;
        instantWorldEv.Entity = null;

        _adminLogger.Add(LogType.Action,
            $"{ToPrettyString(user):user} is performing the {Name(ent):action} action provided by {ToPrettyString(provider):provider}.");

        args.Fallback = true;
    }
}
