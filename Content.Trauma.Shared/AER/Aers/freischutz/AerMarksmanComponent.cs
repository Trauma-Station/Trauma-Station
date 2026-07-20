// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

/// <summary>
/// Component for Aer-1821, lets them summon a restricted devil contract
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AerMarksmanComponent : Component
{
    [DataField]
    public EntProtoId ContractPrototype = "AerContract";
}
