using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Stores information about a set of constructed object
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KnowledgeConstructionModifierComponent : Component
{
    /// <summary>
    /// Stores the difference between levels s as to attribute qualities onto the item.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, int> LevelDeltas = new();
}
