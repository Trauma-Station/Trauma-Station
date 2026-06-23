using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NetpodComponent : Component
{
    [DataField]
    public EntityUid? LinkedServer;

    [DataField]
    public EntityUid? Occupant;

    [DataField]
    public EntityUid? Avatar;

    [DataField]
    public TimeSpan AutoConnectDelay = TimeSpan.FromSeconds(1.8);

    [DataField]
    public bool DeployedAvatar;

    /// <summary>
    /// Internal re-entrancy guard while removing occupant from the pod container.
    /// </summary>
    public bool EjectingOccupant;

    [DataField, AutoNetworkedField]
    public ProtoId<StartingGearPrototype>? PreferredLoadout = "BitrunnerAvatarGear";

    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, ProtoId<StartingGearPrototype>> PreferredLoadoutSpecie = new();

    [DataField]
    public List<ProtoId<StartingGearPrototype>> AllowedLoadout = new();

    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, List<ProtoId<StartingGearPrototype>>> AllowedLoadoutSpecie = new();

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
