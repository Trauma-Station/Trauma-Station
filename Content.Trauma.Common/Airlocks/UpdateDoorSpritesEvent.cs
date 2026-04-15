// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Airlocks;

[ByRefEvent]
public record struct UpdateDoorSpritesEvent(EntityPrototype Proto, bool Handled = false);
