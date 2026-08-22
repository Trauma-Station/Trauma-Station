// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Audio;

namespace Content.Trauma.Common.Bank;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MoneyStorageComponent : Component
{
    [DataField]
    public SoundSpecifier? SoundOnInsertMoney = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg", AudioParams.Default.WithVariation(0.05f));

    [DataField(required: true)]
    public ProtoId<CurrencyPrototype> Currency = "Spesos";

    [DataField, AutoNetworkedField]
    public FixedPoint2 StoredMoney;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MoneyBuffer;
}
