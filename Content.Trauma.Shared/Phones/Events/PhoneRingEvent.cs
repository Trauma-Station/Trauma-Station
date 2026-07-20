// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Phones.Components;

namespace Content.Trauma.Shared.Phones.Events;

[ByRefEvent]
public record struct PhoneRingEvent(Entity<RotaryPhoneComponent> Phone);
