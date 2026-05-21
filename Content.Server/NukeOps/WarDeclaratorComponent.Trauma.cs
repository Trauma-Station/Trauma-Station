using Robust.Shared.Audio;

namespace Content.Server.NukeOps;

public sealed partial class WarDeclaratorComponent : Component
{
    [DataField]
    public SoundSpecifier Music = new SoundPathSpecifier("/Audio/_Goobstation/Announcements/war.ogg");
}
