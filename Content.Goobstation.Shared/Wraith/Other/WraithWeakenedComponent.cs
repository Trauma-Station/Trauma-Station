// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Wraith.Other;

[RegisterComponent, NetworkedComponent]
public sealed partial class WraithWeakenedComponent : Component;

[ByRefEvent]
public record struct WraithWeakenedAddedEvent;

[ByRefEvent]
public record struct WraithWeakenedRemovedEvent;
