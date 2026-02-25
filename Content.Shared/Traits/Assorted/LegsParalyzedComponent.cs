using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Set player speed to zero and standing state to down, simulating leg paralysis.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LegsParalyzedSystem))]
[AutoGenerateComponentState] // Trauma
public sealed partial class LegsParalyzedComponent : Component
{
    // <Trauma>
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public float WalkSpeedModifier = 0.5f;

    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public float SprintSpeedModifier = 0.5f;
    // </Trauma>
}
