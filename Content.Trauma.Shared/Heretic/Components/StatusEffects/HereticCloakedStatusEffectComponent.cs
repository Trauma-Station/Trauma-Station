// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticCloakedStatusEffectComponent : Component
{
    [DataField]
    public bool RequiresFocus = true;

    [DataField]
    public LocId? LoseFocusMessage;

    [DataField]
    public SoundSpecifier? CloakSound = new SoundCollectionSpecifier("Curse");

    [DataField]
    public SoundSpecifier? UncloakSound = new SoundCollectionSpecifier("Curse");
}
