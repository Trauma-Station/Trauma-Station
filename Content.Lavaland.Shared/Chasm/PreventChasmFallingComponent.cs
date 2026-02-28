// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Lavaland.Shared.Chasm;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PreventChasmFallingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool DeleteOnUse = true;

    [DataField]
    public SoundSpecifier? TeleportSound = new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg");
}
