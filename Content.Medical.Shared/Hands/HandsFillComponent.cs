// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Hands;

/// <summary>
/// Creates a list of hands and spawns items to fill them.
/// </summary>
[RegisterComponent, Access(typeof(HandsFillSystem))]
public sealed partial class HandsFillComponent : Component
{
    /// <summary>
    /// The name of each hand and the item to fill it with, if any.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, EntProtoId?> Hands = new();
}
