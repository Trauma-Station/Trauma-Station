// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Clothing.Sealable;

/// <summary>
/// Defines the clothing entity that can be sealed by <see cref="SealableClothingControlComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SealableClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsSealed;

    [DataField]
    public TimeSpan SealingTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public LocId SealUpPopup = "sealable-clothing-seal-up";

    [DataField]
    public LocId SealDownPopup = "sealable-clothing-seal-down";

    [DataField]
    public SoundSpecifier SealUpSound = new SoundPathSpecifier("/Audio/Mecha/mechmove03.ogg");

    [DataField]
    public SoundSpecifier SealDownSound = new SoundPathSpecifier("/Audio/Mecha/mechmove03.ogg");
}
