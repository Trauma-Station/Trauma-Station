// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.BloodCult.Gamerule;

/// <summary>
/// Component given to a blood cult's sacrifice target.
/// Used to reroll the target if they get deleted.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class BloodCultTargetComponent : Component
{
    [DataField]
    public EntityUid Rule;

    /// <summary>
    /// How long you can be off station until it picks another target.
    /// </summary>
    [DataField]
    public TimeSpan ExileTime = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? NextExiled;
}
