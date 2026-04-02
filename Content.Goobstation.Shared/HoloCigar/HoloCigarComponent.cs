// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.HoloCigar;

/// <summary>
/// This is used by the man who sold the world.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HoloCigarComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Lit;

    [DataField]
    public SoundSpecifier Music = new SoundPathSpecifier(
        "/Audio/_Goobstation/Items/TheManWhoSoldTheWorld/invisibingle.ogg",
        new AudioParams().WithLoop(true).WithVolume(-3f));

    [DataField, AutoNetworkedField]
    public EntityUid? MusicEntity;
}
