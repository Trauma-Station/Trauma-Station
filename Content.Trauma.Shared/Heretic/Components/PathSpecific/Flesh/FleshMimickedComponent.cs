// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Flesh;

[RegisterComponent, NetworkedComponent]
public sealed partial class FleshMimickedComponent : Component
{
    [DataField]
    public List<EntityUid> FleshMimics = new();
}
