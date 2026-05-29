// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Crucible.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class XRayVisionStatusEffectComponent : Component
{
    [DataField]
    public bool ShouldRemoveFov = true;
}
