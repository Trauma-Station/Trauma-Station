using Content.Shared.Whitelist;

namespace Content.Shared.Salvage.Fulton;

public sealed partial class FultonComponent
{
    /// <summary>
    /// Blacklist for entities it cannot fulton.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist = new()
    {
        Components = ["FultonBlacklist"]
    };

    /// <summary>
    /// Whether <see cref="Beacon"/> is set on the server.
    /// Needed since when its far away or on a different map it won't be in PVS, which is the usecase of fultons.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasBeacon;

    /// <summary>
    /// Set to false to disable attaching fultons when clicking entities.
    /// </summary>
    [DataField]
    public bool AttachOnInteract = true;
}
