// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Funeral.Systems;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Funeral.Components;

/// <summary>
/// Component marking entities to turn to holy ash when cremated.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(FuneralAddSystem))]
public sealed partial class FuneralHolyComponent : Component;
