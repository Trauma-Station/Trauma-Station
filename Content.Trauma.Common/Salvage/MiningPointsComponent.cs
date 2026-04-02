// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Salvage;

/// <summary>
/// Stores mining points for a holder, such as an ID card or ore processor.
/// Mining points are gained by smelting ore and redeeming them to your ID card.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CommonMiningPointsSystem))]
[AutoGenerateComponentState]
public sealed partial class MiningPointsComponent : Component
{
    /// <summary>
    /// The number of points stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public uint Points;
}
