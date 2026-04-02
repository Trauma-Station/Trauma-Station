// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class CosmicEntropyConditionComponent : Component
{
    /// <summary>
    ///     The amount of entropy this objective would like to be siphoned
    /// </summary>
    [DataField]
    public int Siphoned;
}
