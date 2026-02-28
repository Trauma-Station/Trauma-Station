// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Flashbang;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlashSoundSuppressionComponent : Component
{
    [DataField]
    public float ProtectionRange = 2f;
}
