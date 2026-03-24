// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.ClockworkCult.Scripture;

/// <summary>
/// Attach this to anything that you want to appear in the Clockwork Slab
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ScriptureSystem))]
public sealed partial class ScriptureComponent : Component
{
    /// <summary>
    /// The power it costs to cast this scripture, measured in Watts
    /// </summary>
    [DataField]
    public int PowerCost = 1000;
};
