using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Body.Part;

/// <summary>
/// Gives this body part a cavity that can be inserted and taken out via surgery.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BodyPartCavitySystem))]
public sealed partial class BodyPartCavityComponent : Component
{
    /// <summary>
    /// Name of the container slot to add.
    /// </summary>
    [DataField]
    public string ContainerId = "_part_cavity";

    /// <summary>
    /// If non-null, items must match this whitelist to be inserted.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// If non-null, items cannot match this blacklist to be inserted.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
