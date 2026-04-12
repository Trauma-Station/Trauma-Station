// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.MartialArts;

namespace Content.Trauma.Shared.MartialArts.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabStagesOverrideComponent : Component
{
    [DataField]
    public GrabStage StartingStage = GrabStage.Soft;
}
