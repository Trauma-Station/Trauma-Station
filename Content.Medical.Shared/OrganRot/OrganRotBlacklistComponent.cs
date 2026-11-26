// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Shared.OrganRot;

/// <summary>
/// Used for cybernetics and other organs that should not rot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganRotBlacklistComponent : Component;
