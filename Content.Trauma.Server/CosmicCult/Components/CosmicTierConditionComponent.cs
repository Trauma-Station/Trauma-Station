// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class CosmicTierConditionComponent : Component
{
    [DataField]
    public int Tier;
}
