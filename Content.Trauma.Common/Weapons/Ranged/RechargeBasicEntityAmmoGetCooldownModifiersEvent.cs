// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Weapons.Ranged;

// todo: get event names closer to the length of the bible
[ByRefEvent]
public record struct RechargeBasicEntityAmmoGetCooldownModifiersEvent(float Multiplier);
