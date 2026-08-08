using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

public sealed partial class DamageContactsComponent
{
    /// <summary>
    /// The sound to play when damage is done
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundPathSpecifier? DamageSound;
}
