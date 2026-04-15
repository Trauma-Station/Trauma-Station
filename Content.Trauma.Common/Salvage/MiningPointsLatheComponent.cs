// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Salvage;

/// <summary>
/// Adds points to <see cref="MiningPointsComponent"/> when making a recipe that has miningPoints set.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MiningPointsLatheComponent : Component;
