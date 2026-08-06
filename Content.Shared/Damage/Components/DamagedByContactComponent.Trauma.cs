using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

public sealed partial class DamagedByContactComponent
{
    /// <summary>
    /// The sound to play when damage is done
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundPathSpecifier? DamageSound;
}
