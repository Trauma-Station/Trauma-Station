// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.StationRadio.Components;

/// <summary>
/// Added to vinyls when inserted to track when the song ends for effects like summon rule.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ActiveVinylComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan EndTime;

    [DataField]
    public EntityUid Player;
}
