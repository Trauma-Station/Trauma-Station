namespace Content.Shared.Traits.Assorted;

public sealed partial class LegsParalyzedComponent
{
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public float WalkSpeedModifier = 0.5f;

    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public float SprintSpeedModifier = 0.5f;

    /// <summary>
    /// If true, kills user legs, which makes them cripple forever, even after component removal
    /// If false, use <see cref="WalkSpeedModifier"/> and <see cref="SprintSpeedModifier"/> to modify speed instead
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool Permanent = true;
}
