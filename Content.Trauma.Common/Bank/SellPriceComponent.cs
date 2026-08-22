// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Trauma.Common.Bank;

[RegisterComponent, NetworkedComponent]
public sealed partial class SellPriceComponent : Component
{
    [DataField]
    public FixedPoint2 Price;
}
