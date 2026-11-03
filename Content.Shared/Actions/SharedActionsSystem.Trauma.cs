using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Database;
using Content.Shared.Ghost.Components;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    /// <summary>
    /// Performs an action WITH all condition checks.
    /// </summary>
    public bool TryPerformAction(EntityUid user, RequestPerformActionEvent ev)
    {
        if (!_actionsQuery.TryComp(user, out var component))
            return false;

        var actionEnt = GetEntity(ev.Action);
        if (GetAction(actionEnt) is not {} action)
            return false;

        if (!CanPerformAction((user, component), action, ev))
            return false;

        // All checks passed. Perform the action!
        return PerformAction((user, component), action);
    }

    /// <summary>
    /// Runs all checks to see if user currently can perform some action.
    /// </summary>
    public bool CanPerformAction(Entity<ActionsComponent?> user, Entity<ActionComponent> action, RequestPerformActionEvent ev)
    {
        if (!Resolve(user.Owner, ref user.Comp, false)
            || !TryComp(action, out MetaDataComponent? metaData))
            return false;

        var name = Name(action, metaData);

        // Does the user actually have the requested action?
        if (!user.Comp.Actions.Contains(action))
        {
            _adminLogger.Add(LogType.Action,
                $"{ToPrettyString(user):user} attempted to perform an action that they do not have: {name}.");
            return false;
        }

        DebugTools.Assert(action.Comp.AttachedEntity == user);
        if (!action.Comp.Enabled)
            return false;

        var curTime = GameTiming.CurTime;
        if (IsCooldownActive(action, curTime))
            return false;

        // check for action use prevention
        // TODO: make code below use this event with a dedicated component
        var target = GetEntity(ev.EntityTarget);
        var attemptEv = new ActionAttemptEvent(user, target);
        RaiseLocalEvent(action, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        // Validate request by checking action blockers and the like
        var provider = action.Comp.Container ?? user;
        var validateEv = new ActionValidateEvent()
        {
            Input = ev,
            User = user,
            Provider = provider
        };
        RaiseLocalEvent(action, ref validateEv);
        return !validateEv.Invalid;
    }

    public virtual void SaveActions(EntityUid performer)
    {
    }

    public virtual void LoadActions(EntityUid performer)
    {
    }
}
