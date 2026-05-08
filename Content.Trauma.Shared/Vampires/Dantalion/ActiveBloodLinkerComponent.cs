// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Vampires.Dantalion;

/// <summary>
/// Marks a vampire as an active user of blood bond.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveBloodLinkerComponent : Component
{
    /// <summary>
    /// How much blood we are gonna drain from this blood bond linker.
    /// </summary>
    [DataField]
    public int BloodDrain;

    /// <summary>
    /// How often to drain blood.
    /// </summary>
    [DataField]
    public TimeSpan Update = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// The blood link action. Used for toggling it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Action;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;
}
