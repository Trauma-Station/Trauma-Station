namespace Content.Shared.Actions.Components;

public sealed partial class ConfirmableActionComponent
{
    /// <summary>
    /// Whether this action should cancel itself to confirm or not
    /// </summary>
    [DataField]
    public bool ShouldCancel = true;
}
