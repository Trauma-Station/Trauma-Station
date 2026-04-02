// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Phones.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RotaryPhoneHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? PhoneNumber;

    [DataField, AutoNetworkedField]
    public EntityUid? ConnectedPhone;

    [DataField]
    public string Name = "Unknown";

    [DataField]
    public SpriteSpecifier RopeSprite = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Objects/phone/phone.rsi"), "rope");
}
