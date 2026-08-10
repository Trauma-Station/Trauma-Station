// SPDX-License-Identifier: AGPL-3.0-or-later


using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Wizard.FadingTimedDespawn;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState(true)]
public sealed partial class FadingTimedDespawnComponent : Component
{
    /// <summary>
    /// How long the entity will exist before despawning
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// If it is above zero, entity will fade out slowly when despawning
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FadeOutTime = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan Timer;

    /// <summary>
    /// Whether this entity started to fade out
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool FadeOutStarted;

    public const string AnimationKey = "fadeout";
}
