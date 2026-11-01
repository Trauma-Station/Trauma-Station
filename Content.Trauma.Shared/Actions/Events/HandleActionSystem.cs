namespace Content.Trauma.Shared.Actions.Events;

/// <summary>
/// System that automatically handles target actions, if you ever find this useful.
/// Currently used for some megafauna actions.
/// </summary>
public sealed partial class HandleActionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnHandleInstantAction(HandleInstantActionEvent args)
    {
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnHandleTargetAction(HandleTargetActionEvent args)
    {
        args.Handled = true;
    }
}
