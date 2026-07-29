using Content.Trauma.Common.CartridgeLoader.Cartridges;

namespace Content.Shared.CartridgeLoader.Cartridges;

public sealed partial class LogProbeCartridgeComponent
{
    /// <summary>
    /// The last scanned NanoChat data, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public NanoChatData? ScannedNanoChatData;
}
