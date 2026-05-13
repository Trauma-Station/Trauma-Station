namespace Content.Shared.Tools.Components;

public sealed partial class ToolOpenableComponent
{
    /// <summary>
    ///     If true, will not show examine when examined
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowExamine;
}
