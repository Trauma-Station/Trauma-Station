// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.DoAfter;

/// <summary>
/// Component for bodyparts that change doafter delay length.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DoAfterDelayMultiplierComponent : Component
{
    [DataField(required: true)]
    public float Multiplier = 1f;
}
