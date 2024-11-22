// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Spells;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultTeleportActionComponent : Component
{
    [DataField]
    public SoundSpecifier? TeleportInSound;

    [DataField]
    public SoundSpecifier? TeleportOutSound;
}
