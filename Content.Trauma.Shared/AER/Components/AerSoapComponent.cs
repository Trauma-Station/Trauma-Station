// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

/// <summary>
/// component that identifies a entity that follows the aer research behaviour of Aer Soap:
/// produce rd and gear on slipping someone
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AerSoapComponent : Component;
