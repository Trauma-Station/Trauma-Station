// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Communications;

/// <summary>
/// Entities with <see cref="TelecomTransmitterComponent"/> are needed to transmit messages using headsets BETWEEN MAPS if they are powered.
/// </summary>
[RegisterComponent]
public sealed partial class TelecomTransmitterComponent : Component;
