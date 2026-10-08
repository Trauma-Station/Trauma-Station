// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Factory.Shared.Access;

/// <summary>
/// Prevents an ID card from being detected by an <see cref="AccessScannerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AccessScannerBlacklistComponent : Component;
