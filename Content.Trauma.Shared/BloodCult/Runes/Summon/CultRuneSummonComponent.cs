// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Runes.Summon;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultRuneSummonComponent : Component
{
    [DataField]
    public SoundPathSpecifier TeleportSound = new("/Audio/_Trauma/BloodCult/veilin.ogg");
}
