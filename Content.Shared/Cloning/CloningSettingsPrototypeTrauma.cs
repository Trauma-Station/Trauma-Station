namespace Content.Shared.Cloning;

public sealed partial class CloningSettingsPrototype
{
    [DataField]
    public bool MakeEquipmentUnremoveable;

    [DataField]
    public bool CopyStorage = true;

    [DataField]
    public bool InternalContentsUnremoveable;

    [DataField]
    public bool AllowNonHumanoid;
}
