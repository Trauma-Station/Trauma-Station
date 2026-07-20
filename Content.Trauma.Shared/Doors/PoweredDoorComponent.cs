// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Doors;

/// <summary>
/// Prevents prying while powered without requiring all of the airlock logic.
/// Also lets you pry it on click when unpowered with no tools, like ClickOpen doors but without making it open on click when powered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PoweredDoorComponent : Component;
