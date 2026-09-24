// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Funeral.Components;

/// <summary>
/// Component marking entities to turn to holy ash when cremated.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FuneralHolyComponent : Component;
