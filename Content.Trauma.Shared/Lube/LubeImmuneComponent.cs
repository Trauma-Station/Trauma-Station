// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Lube;

/// <summary>
/// Component for mobs or gloves that makes them unaffected by lubed items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LubeImmuneComponent : Component;
