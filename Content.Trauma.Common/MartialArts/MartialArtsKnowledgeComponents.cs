// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Trauma.Common.MartialArts;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabStagesOverrideComponent : Component
{
    [DataField]
    public GrabStage StartingStage = GrabStage.Soft;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MartialArtsKnowledgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Blocked;

    [DataField, AutoNetworkedField]
    public int TemporaryBlockedCounter;

    [DataField(required: true)]
    public SpriteSpecifier Icon;
}
