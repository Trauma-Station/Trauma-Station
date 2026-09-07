// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Gateway;

/// <summary>
/// Controlling gateway that links to other gateway destinations on the server.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GatewaySystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class GatewayComponent : Component
{
    /// <summary>
    /// Whether this destination is shown in the gateway ui.
    /// If you are making a gateway for an admeme set this once you are ready for players to select it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Can the gateway be interacted with? If false then only settable via admins / mappers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Interactable = true;

    /// <summary>
    /// Name as it shows up on the ui of station gateways.
    /// </summary>
    [DataField]
    public FormattedMessage Name = new();

    /// <summary>
    /// Sound to play when opening the portal.
    /// </summary>
    [DataField]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg");

    /// <summary>
    /// Sound to play when closing the portal.
    /// </summary>
    [DataField]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg");

    /// <summary>
    /// Sound to play when trying to open or close the portal and missing access.
    /// </summary>
    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    /// <summary>
    /// Cooldown between opening portal / closing.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The time at which the portal can next be opened.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextReady;

    /// <summary>
    /// Restrict this gate's destinations and sources to gates tagged with this.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype>? TagRestriction;
}
