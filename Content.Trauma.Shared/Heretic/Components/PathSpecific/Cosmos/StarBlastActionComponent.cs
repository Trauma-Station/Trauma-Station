// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StarBlastActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Projectile;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(25);

    [DataField]
    public EntProtoId Effect = "EffectCosmicCloud";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/cosmic_energy.ogg");
}
