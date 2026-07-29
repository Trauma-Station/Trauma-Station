// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Materials;

/// <summary>
/// Tries to distribute inserted materials with the first powered master silo on the same grid as this material storage.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MasterSiloFeederComponent : Component;
