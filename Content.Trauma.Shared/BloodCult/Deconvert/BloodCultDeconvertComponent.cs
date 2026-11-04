// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.BloodCult.Deconvert;

/// <summary>
/// Handles blood cultist deconversion by holy water
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BloodCultDeconvertComponent : Component
{
    /// <summary>
    /// How long does deconversion take
    /// </summary>
    [DataField]
    public TimeSpan DeconversionTime = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Time when we started deconversion
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DeconversionStartTime;

    /// <summary>
    /// Time when cultist gets deconverted
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DeconversionEndTime;

    /// <summary>
    /// Effects applied when deconversion proccess begins
    /// </summary>
    [DataField]
    public ProtoId<EntityEffectPrototype> EffectOnStart = "BloodCultDeconversionStart";

    /// <summary>
    /// Effects applied when deconversion is progressing
    /// </summary>
    [DataField]
    public ProtoId<EntityEffectPrototype> EffectOnProgress = "BloodCultDeconversionProgress";

    /// <summary>
    /// Every x seconds we apply effects and check if deconversion is done
    /// </summary>
    [DataField]
    public TimeSpan UpdateTime = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Increases <see cref="DeconversionEndTime"/> by this amount if user is hit by ritual dagger
    /// </summary>
    [DataField]
    public TimeSpan ProgressReductionRate = TimeSpan.FromSeconds(4);
}
