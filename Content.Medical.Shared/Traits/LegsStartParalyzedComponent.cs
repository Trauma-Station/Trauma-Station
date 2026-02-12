// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Traits;

/// <summary>
/// Iterate through all the legs on the entity and prevent them from working.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LegsStartParalyzedComponent : Component;
