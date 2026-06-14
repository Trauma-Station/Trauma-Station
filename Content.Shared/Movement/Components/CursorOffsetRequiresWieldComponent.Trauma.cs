namespace Content.Shared.Movement.Components;

public sealed partial class CursorOffsetRequiresWieldComponent
{
    /// <summary>
    /// Multiplies your viewcone angle by this number when wielded.
    /// </summary>
    [DataField]
    public float ViewAngleMultiplier = 0.3f;
}
