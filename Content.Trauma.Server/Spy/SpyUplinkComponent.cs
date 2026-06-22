using Robust.Shared.Audio;

namespace Content.Trauma.Server.Spy;

[RegisterComponent]
public sealed partial class SpyUplinkComponent : Component
{
    [DataField]
    public EntityUid OwnerMind;

    [DataField]
    public SoundSpecifier StealStartSound = new SoundPathSpecifier("/Audio/_Trauma/Effects/pshoom.ogg");

    [DataField]
    public SoundSpecifier StealEndSound = new SoundPathSpecifier("/Audio/_Trauma/Effects/wewewew.ogg");
}
