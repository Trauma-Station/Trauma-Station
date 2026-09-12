// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// Component for Aer-1821, lets them summon a restricted devil contract
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AerMarksmanComponent : Component
{
    [DataField]
    public EntProtoId ContractPrototype = "AerContract";

    [DataField, AutoNetworkedField]
    public int Souls;

    /// <summary>
    /// Sound effect played when summoning a contract.
    /// </summary>
    [DataField]
    public SoundPathSpecifier FwooshPath = new("/Audio/_Goobstation/Effects/fwoosh.ogg");
}
