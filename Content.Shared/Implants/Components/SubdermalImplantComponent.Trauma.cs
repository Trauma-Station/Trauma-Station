
using Content.Shared.Whitelist;

namespace Content.Shared.Implants.Components;

public sealed partial class SubdermalImplantComponent : Component
{
    /// <summary>
    /// Target MindRole Whitelist for this implant specifically.
    /// </summary>
    [DataField]
    public EntityWhitelist? MindRoleWhitelist;

    /// <summary>
    /// Target MindRole Blacklist for this implant specifically.
    /// </summary>
    [DataField]
    public EntityWhitelist? MindRoleBlacklist;
}
