// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Salvage;

/// <summary>
/// Message for a lathe to transfer its mining points to the user's id card.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheClaimMiningPointsMessage : BoundUserInterfaceMessage;
