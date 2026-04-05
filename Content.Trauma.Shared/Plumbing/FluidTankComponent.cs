using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Plumbing;

[RegisterComponent, NetworkedComponent]
public sealed partial class FluidTankComponent : Component
{
    /// <summary>
    /// Buffer name for solution container.
    /// </summary>
    [DataField]
    public string BufferName = "tank_buffer";

    /// <summary>
    /// The plumbing node name.
    /// </summary>
    [DataField]
    public string NodeName = "fluid";

    /// <summary>
    /// How much can a player transfer per click?
    /// </summary>
    [DataField] public FixedPoint2 TransferAmount = 100;
}
