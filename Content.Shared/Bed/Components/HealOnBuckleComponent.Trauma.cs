using Content.Shared.Whitelist;

namespace Content.Shared.Bed.Components;

public sealed partial class HealOnBuckleComponent
{
    /// <summary>
    /// Blacklist for mobs that can't be healed.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist = new()
    {
        Components = ["Silicon"]
    };
}
