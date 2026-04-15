// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent]
public sealed partial class IceSpearComponent : Component
{
    [DataField]
    public EntityUid? ActionId;

    [DataField]
    public SoundSpecifier ShatterSound = new SoundCollectionSpecifier("GlassBreak");

    [DataField]
    public TimeSpan ShatterCooldown = TimeSpan.FromSeconds(45);
}
