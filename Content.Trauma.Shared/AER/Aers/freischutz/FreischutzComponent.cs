// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

/// <summary>
/// Component for Aer-169, lets them summon a restricted devil contract
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FreischutzComponent : Component
{
    [DataField]
    public EntProtoId ContractPrototype = "AerContract";
}
