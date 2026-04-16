// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Salvage.Systems;
using Content.Shared.Thief;
namespace Content.Trauma.Shared.Salvage.Components;

/// </summary>
///     A vendor that sells mining equipment. Also holds a radial menu to redeem vouchers with a preset list of items.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MiningVoucherSystem))]
public sealed partial class MiningVendorComponent : Component
{
    /// <summary>
    /// The kits that can be selected.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<ThiefBackpackSetPrototype>> Kits = new();
}
