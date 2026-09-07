namespace Content.Shared.Mind.Components;

public sealed partial class MindContainerComponent
{
    /// <summary>
    ///     The mind that used to control the mob this mob, if <see cref="Mind"/> is null. Client doesn't see this
    /// </summary>
    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public EntityUid? OldMind;

    /// <summary>
    ///     True if we had mind at some point, false otherwise.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool HadMind;
}
