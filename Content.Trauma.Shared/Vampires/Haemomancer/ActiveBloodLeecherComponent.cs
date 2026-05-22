// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Vampires.Haemomancer;

/// <summary>
/// Components that does blood bringer's rite behavior.
///
/// When active: constantly leech blood from up to 10 people in view,
/// healing you massively and removing incapacitating effects. The healing is per person in range, and is greatly reduced for burn damage.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveBloodLeecherComponent : Component
{
    /// <summary>
    /// The range of the lookup
    /// </summary>
    [DataField]
    public float Range = 10f;

    /// <summary>
    /// The maximum amount of entities we will drain blood from.
    /// </summary>
    [DataField]
    public int MaxEntities = 10;

    /// <summary>
    /// The blood required to cast this action (notifies the vampire).
    /// </summary>
    [DataField]
    public int BloodRequired = 10;

    /// <summary>
    /// Effects that will run on the user, and scale based on how many entities the lookup got.
    /// </summary>
    [DataField]
    public EntityEffect[]? UserEffects;

    /// <summary>
    /// Effects that will run on the targets.
    /// </summary>
    [DataField]
    public EntityEffect[]? TargetEffects;

    /// <summary>
    /// How often to run a lookup
    /// </summary>
    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// The music to play during the action
    /// </summary>
    [DataField]
    public SoundSpecifier? Music;

    /// <summary>
    /// The music entity, used to kill it when the component shutdowns.
    /// </summary>
    [DataField]
    public EntityUid? MusicEntity;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdate;
}
