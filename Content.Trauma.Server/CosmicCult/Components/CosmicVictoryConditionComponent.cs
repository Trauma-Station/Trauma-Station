// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class CosmicVictoryConditionComponent : Component
{
    [DataField]
    public bool Victory;
}
