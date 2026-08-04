using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Spy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpyUplinkComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField, AutoNetworkedField]
    public EntityUid OwnerMind;

    [DataField]
    public SoundSpecifier StealStartSound = new SoundPathSpecifier("/Audio/_Trauma/Effects/pshoom.ogg");

    [DataField]
    public SoundSpecifier StealEndSound = new SoundPathSpecifier("/Audio/_Trauma/Effects/wewewew.ogg");
}
