using Content.Shared.Whitelist;

namespace Content.Trauma.Client.Heretic;

[RegisterComponent]
public sealed partial class BlockContextMenuComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
