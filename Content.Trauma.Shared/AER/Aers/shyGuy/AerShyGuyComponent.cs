// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// Component for Aer-1821, lets them summon a restricted devil contract
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AerShyGuyComponent : Component
{
    /// <summary>
    /// stolen from slasher i'll rename it to isFaceCovered
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsIncorporeal;

    /// <summary>
    /// Range (in tiles) to check for observers with line of sight that prevent incorporealizing. might need more for idiots with sniper rifles/binoculars
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ObserverCheckRange = 10f;

    //set of all the EntityUid that dared to see poor shyguy
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> KillList = [];

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextCheck;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Sound effect played when shyguy is gazed upon.
    /// </summary>
    [DataField]
    public SoundPathSpecifier Scream = new("/Audio/Voice/Human/malescream_1.ogg");
}
