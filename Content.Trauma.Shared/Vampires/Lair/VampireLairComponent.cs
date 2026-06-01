// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Vampires.Lair;

/// <summary>
/// Used on coffins. Holds data on whether this coffin has a vampire owner.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class VampireLairComponent : Component
{
    /// <summary>
    /// The vampire that owns this coffin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Vampire;

    /// <summary>
    /// This status effect applies to the <see cref="Vampire"/> once they get inserted into the lair.
    /// Gets removed on removal from the lair container.
    /// </summary>
    [DataField]
    public EntProtoId CoffinStatus = "VampireLairStatusEffect";

    /// <summary>
    /// How often to send popup to the <see cref="Vampire"/>.
    /// </summary>
    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(3f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextPopup;
}
