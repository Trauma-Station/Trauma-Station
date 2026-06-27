namespace Content.Shared.Tools.Components;

public sealed partial class ToolComponent
{
    /// <summary>
    /// Whether to check doafter validity every tick even if we don't satisfy the usual conditions.
    /// </summary>
    [DataField]
    public bool AlwaysCheckDoAfter;

    /// <summary>
    /// Whether to show tool qualities when examined.
    /// </summary>
    [DataField]
    public bool Examinable = true;
}
