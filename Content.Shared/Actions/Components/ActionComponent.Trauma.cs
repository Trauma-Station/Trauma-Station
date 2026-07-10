namespace Content.Shared.Actions.Components;

public sealed partial class ActionComponent
{
    /// <summary>
    /// Raise event on the action entity instead of user/container.
    /// </summary>
    [DataField]
    public bool RaiseOnAction;

    /// <summary>
    /// If true, ghosts will be granted this action.
    /// For wizard lich revival stuff.
    /// </summary>
    [DataField]
    public bool AllowGhostAction;

    /// <summary>
    /// Is this action predicted?
    /// </summary>
    [DataField]
    public bool Predicted = true;

    /// <summary>
    /// If true, this means action won't get removed when user unequips action container item
    /// </summary>
    [DataField]
    public bool DetachedItemAction;
}
