using Content.Shared.Actions;
using Content.Shared.Resist;
using Robust.Shared.Prototypes;

namespace Content.Server.Resist;

public sealed partial class EscapeInventorySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    /// <summary>
    /// You can't escape the hands of an entity this many times more massive than you.
    /// </summary>
    public const float MaximumMassDisadvantage = 6f;

    private static readonly EntProtoId _escapeCancelAction = "ActionCancelEscape";

    [SubscribeLocalEvent]
    private void OnCancelEscape(Entity<CanEscapeInventoryComponent> ent, ref EscapeInventoryCancelActionEvent args)
    {
        if (ent.Comp.DoAfter is { } doAfter)
            _doAfterSystem.Cancel(doAfter);

        _actions.RemoveAction(ent.Owner, ent.Comp.EscapeCancelAction);
        ent.Comp.EscapeCancelAction = null;
    }
}
