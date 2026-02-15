using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Set player speed to zero and standing state to down, simulating leg paralysis.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LegsParalyzedSystem))]
public sealed partial class LegsParalyzedComponent : Component
{
    // <Trauma>
    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public float WalkSpeed;

    [DataField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public float SprintSpeed;
    // </Trauma>
}
