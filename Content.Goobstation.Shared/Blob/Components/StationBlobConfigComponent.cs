// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Blob.Components;

/// <summary>
/// Station component that controls blob win conditions.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StationBlobConfigComponent : Component
{
    public const int DefaultStageBegin = 30;
    public const int DefaultStageCritical = 400;
    public const int DefaultStageEnd = 800;

    [DataField, AutoNetworkedField]
    public int StageBegin { get; set; } = DefaultStageBegin;

    [DataField, AutoNetworkedField]
    public int StageCritical { get; set; } = DefaultStageCritical;

    [DataField, AutoNetworkedField]
    public int StageTheEnd { get; set; } = DefaultStageEnd;
}
