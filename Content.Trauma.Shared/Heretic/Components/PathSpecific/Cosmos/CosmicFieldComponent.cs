// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CosmicFieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Strength;

    [DataField]
    public SoundSpecifier BombDefuseSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    [DataField]
    public LocId BombDefusePopup = "cosmic-field-component-bomb-defused-message";

    [DataField]
    public ProtoId<TagPrototype> IgnoredTag = "GrenadeIgnoreCosmicField";
}
