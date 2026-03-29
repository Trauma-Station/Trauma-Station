namespace Content.Shared.Cargo.Components;

/// <summary>
/// Trauma - explicit destination setting for orders
/// </summary>
public sealed partial class CargoOrderConsoleComponent
{
    /// <summary>
    /// Telepad to send orders to, null for the ATS.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public NetEntity? Destination;

    /// <summary>
    /// Allows disabling access check for the target account.
    /// </summary>
    [DataField]
    public bool IgnoreAccess;
}
