using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem
{
    /// <summary>
    /// Tries to get an action by proto id.
    /// </summary>
    public bool TryGetActionById(
        EntityUid ent,
        EntProtoId actionId,
        [NotNullWhen(true)] out Entity<ActionComponent>? action,
        ActionsContainerComponent? container = null)
    {
        action = null;
        var actions = Resolve(ent, ref container, false)
            ? container.Container.ContainedEntities
            : GetActions(ent).Select(x => x.Owner);
        foreach (var uid in actions)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            var entityPrototypeId = MetaData(uid).EntityPrototype?.ID;
            if (entityPrototypeId == null
                || entityPrototypeId != actionId)
                continue;

            action = (uid, Comp<ActionComponent>(uid));
            return true;
        }

        return false;
    }
}
