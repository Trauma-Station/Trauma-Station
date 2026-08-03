using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.Bible.Components;

public sealed partial class BibleComponent
{
    /// <summary>
    /// How much damage to deal to an unholy entity being smitten
    /// </summary>
    [DataField]
    public DamageSpecifier SmiteDamage = new()
    {
        DamageDict = new()
        {
            { "Holy", 25 }
        }
    };

    /// <summary>
    /// How long to stun the entity being smitten
    /// </summary>
    [DataField]
    public TimeSpan SmiteStunDuration = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Which sound to play on heal.
    /// </summary>
    [DataField]
    public SoundSpecifier HealSoundPath = new SoundPathSpecifier("/Audio/Effects/holy.ogg");

    /// <summary>
    /// Which sound to play on damage.
    /// </summary>
    [DataField]
    public SoundSpecifier SizzleSoundPath = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");
}
