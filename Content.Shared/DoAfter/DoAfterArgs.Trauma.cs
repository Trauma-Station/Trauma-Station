namespace Content.Shared.DoAfter;

public sealed partial class DoAfterArgs
{
    /// <summary>
    /// Show doAfter progress bar to another entity
    /// </summary>
    [DataField, NonSerialized]
    public EntityUid? ShowTo;

    public NetEntity? NetShowTo;

    /// <summary>
    /// Whether the delay multiplier event should be raised
    /// </summary>
    [DataField]
    public bool MultiplyDelay = true;

    /// <summary>
    /// If not null, progress bar will use this color.
    /// </summary>
    [DataField]
    public Color? ColorOverride;
}
