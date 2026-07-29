// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Teleportation.Systems;
using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.Teleportation.Components;

/// <summary>
/// Creates a map for a pocket dimension on spawn.
/// When activated by alt verb, spawns a portal to this dimension or closes it.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PocketDimensionSystem))]
[AutoGenerateComponentState]
public sealed partial class PocketDimensionComponent : Component
{
    /// <summary>
    /// Whether this pocket dimension portal is enabled.
    /// </summary>
    [DataField]
    public bool PortalEnabled = false;

    /// <summary>
    /// The portal in the pocket dimension. Created when the entry portal is first opened.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ExitPortal;

    /// <summary>
    /// The pocket dimension map. Created when the entry portal is first opened.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? PocketDimensionMap;

    /// <summary>
    /// The first grid found on <see cref="PocketDimensionMap"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RootGrid;

    /// <summary>
    /// Path to the pocket dimension's map file
    /// </summary>
    [DataField]
    public ResPath PocketDimensionPath = new ResPath("/Maps/_Goobstation/Nonstations/pocket-dimension.yml");

    /// <summary>
    /// The prototype to spawn for the portal spawned in the pocket dimension.
    /// </summary>
    [DataField]
    public EntProtoId ExitPortalPrototype = "DimensionPotExitPortal";

    [DataField]
    public SoundSpecifier OpenPortalSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f)
    };

    [DataField]
    public SoundSpecifier ClosePortalSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
