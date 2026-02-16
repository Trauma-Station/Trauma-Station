// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Prevents removing this organ from a body and logs an error if it somehow happens.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UnremoveableOrganComponent : Component
{
}
