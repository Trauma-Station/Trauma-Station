// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Materials;

/// <summary>
/// Makes this material storage eligible for receiving mats from the <see cref="MasterSiloComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MasterSiloClientComponent : Component;
