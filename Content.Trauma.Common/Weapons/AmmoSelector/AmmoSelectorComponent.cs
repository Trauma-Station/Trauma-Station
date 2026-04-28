// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Common.Weapons.AmmoSelector;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AmmoSelectorComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<SelectableAmmoPrototype>> Prototypes = new();

    [DataField, AutoNetworkedField]
    public ProtoId<SelectableAmmoPrototype>? CurrentlySelected;

    [DataField]
    public SoundSpecifier? SoundSelect = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/selector.ogg");
}
