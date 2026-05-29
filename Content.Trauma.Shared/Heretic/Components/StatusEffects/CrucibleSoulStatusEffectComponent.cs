// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Shared.Heretic.Components.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrucibleSoulStatusEffectComponent : Component
{
    [DataField]
    public EntityCoordinates? Coords;
}
