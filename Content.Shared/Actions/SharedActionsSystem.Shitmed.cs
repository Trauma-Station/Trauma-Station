using System.Linq;
using Content.Shared.Actions.Components;

namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem
{
    /// <summary>
    /// Used to hide actions on an entity
    /// </summary>
    public EntityUid[] HideActions(EntityUid performer, ActionsComponent? comp = null)
    {
        if (!Resolve(performer, ref comp, false))
            return [];

        var actions = comp.Actions.ToArray();
        comp.Actions.Clear();
        Dirty(performer, comp);
        return actions;
    }

    /// <summary>
    /// Used to unhide actions on an entity.
    /// </summary>
    public void UnHideActions(EntityUid performer, EntityUid[] actions, ActionsComponent? comp = null)
    {
        if (!Resolve(performer, ref comp, false))
            return;

        foreach (var action in actions)
            comp.Actions.Add(action);
        Dirty(performer, comp);
    }
}
