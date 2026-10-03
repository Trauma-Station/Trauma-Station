// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Content.Shared.Tag;
using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.Shadowling.Components.Abilities.CollectiveMind;

/// <summary>
/// This is used for the Sonic Screech ability.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowlingSonicScreechComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionSonicScreech";

    [DataField]
    public EntityUid? ActionEnt;

    /// <summary>
    /// The search radius of the ability.
    /// </summary>
    [DataField]
    public float Range = 5f;

    /// <summary>
    /// The amount of time silicons get stunned for, according to <see cref="SiliconWhitelist"/>.
    /// </summary>
    [DataField]
    public TimeSpan SiliconStunTime = TimeSpan.FromSeconds(5);

    [DataField]
    public EntityWhitelist SiliconWhitelist = new()
    {
        Components = ["Silicon", "BorgChassis", "Drone"]
    };

    /// <summary>
    /// Blacklist for mobs that can't be affected by the screech.
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist = new()
    {
        Components =
        [
            "Deaf", // cant hear the screech
            "Thrall",
            "Shadowling"
        ]
    };

    /// <summary>
    /// The tag that indicates that the obstacle hit by the ability is a window.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> WindowTag = "Window";

    /// <summary>
    /// How much damage the window structures take from this ability.
    /// </summary>
    [DataField]
    public DamageSpecifier WindowDamage = new()
    {
        DamageDict = new()
        {
            { "Structural", 50 }
        }
    };

    /// <summary>
    /// The prototype of the flash that gets thrown on the targets of this ability.
    /// </summary>
    [DataField]
    public EntProtoId ProtoFlash = "EffectScreech";

    /// <summary>
    /// The sound that plays once the ability is used.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScreechSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Shadowling/screech.ogg");

    /// <summary>
    /// The effect that is used once the ability activates.
    /// </summary>
    [DataField]
    public EntProtoId SonicScreechEffect = "ShadowlingSonicScreechEffect";
}
