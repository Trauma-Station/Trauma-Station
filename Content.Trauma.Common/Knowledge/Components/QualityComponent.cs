// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Stores the quality info of an object
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class QualityComponent : Component
{
    /// <summary>
    /// Stores the level mastery of the item that will satisfactorily modify it.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<EntProtoId, int> LevelDeltas = new();

    /// <summary>
    /// Stores the quality of the item, which changes some functionality when used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Quality = 0;

    /// <summary>
    /// Stores the amount of items with mastery that have been added together.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int NumberOfMasteries = 0;
}
