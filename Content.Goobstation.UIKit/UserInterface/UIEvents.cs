// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Goobstation.UIKit.UserInterface;

[ByRefEvent]
public readonly record struct ButtonTagPressedEvent(string Id, NetEntity User, NetCoordinates Coords);
