// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticCosmicRuneActionComponent : Component
{
    [DataField]
    public EntityUid? FirstRune;

    [DataField]
    public EntityUid? SecondRune;
}
