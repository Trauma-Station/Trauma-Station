// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

/// <summary>
/// component that identifies a entity that follows the aer research behaviour of the wailing horse:
/// goes inactive on death,
/// produce rd and gear on using it's wail ability
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AerHorseComponent : Component;
