using Content.Shared.Whitelist;

namespace Content.Shared.Radio;

public sealed partial class RadioChannelPrototype
{
    /// <summary>
    /// Whitelist for entities that can send or receive this radio channel messages
    /// </summary>
    [DataField]
    public EntityWhitelist? SendWhitelist;

    [DataField]
    public EntityWhitelist? ReceiveWhitelist;
}
