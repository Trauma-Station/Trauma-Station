// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Bitrunning.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NetpodComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedServer;

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    [DataField, AutoNetworkedField]
    public EntityUid? Avatar;

    [DataField]
    public TimeSpan AutoConnectDelay = TimeSpan.FromSeconds(1.8);

    [DataField, AutoNetworkedField]
    public bool DeployedAvatar;

    [DataField]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/_Orion/Machines/tram/tramopen.ogg", AudioParams.Default.WithVolume(-2f).WithVariation(0.1f));

    [DataField]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/_Orion/Machines/tram/tramclose.ogg", AudioParams.Default.WithVolume(-2f).WithVariation(0.1f));

    [DataField]
    public SoundSpecifier ConnectStasisSound = new SoundPathSpecifier("/Audio/_Orion/Effects/submerge.ogg");

    [DataField]
    public SoundSpecifier ConnectAvatarSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");

    [DataField]
    public SoundSpecifier DisconnectSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");

    [DataField]
    public SoundSpecifier AutoDisconnectSound = new SoundPathSpecifier("/Audio/_Orion/Effects/splash.ogg");

    [DataField]
    public SoundSpecifier OccupiedPrySound = new SoundPathSpecifier("/Audio/_Orion/Machines/airlock/airlock_alien_prying.ogg");

    [DataField]
    public SoundSpecifier OccupiedPryAlertSound = new SoundPathSpecifier("/Audio/_Orion/Machines/terminal/terminal_alert.ogg");
}
