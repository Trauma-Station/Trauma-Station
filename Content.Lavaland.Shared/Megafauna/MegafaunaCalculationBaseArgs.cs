// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Megafauna.Systems;
using Robust.Shared.Random;

namespace Content.Lavaland.Shared.Megafauna;

/// <summary>
/// Arguments that are used for Megafauna Actions and Conditions.
/// </summary>
public record struct MegafaunaCalculationBaseArgs(
    MegafaunaSystem System,
    EntityUid Entity,
    IEntityManager EntMan,
    IPrototypeManager Proto,
    ISawmill Log,
    IRobustRandom Random);
