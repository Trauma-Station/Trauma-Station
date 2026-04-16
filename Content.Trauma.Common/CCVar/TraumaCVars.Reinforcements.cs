// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    /// <summary>
    /// How many seconds regular reinforcement beacons take to open their ghost roles.
    /// </summary>
    public static readonly CVarDef<float> SlowReinforcementDelay =
        CVarDef.Create("trauma.slow_reinforcement_delay", 180f, CVar.SERVER);

    /// <summary>
    /// How many seconds fast reinforcement beacons take to open their ghost roles.
    /// </summary>
    public static readonly CVarDef<float> FastReinforcementDelay =
        CVarDef.Create("trauma.fast_reinforcement_delay", 30f, CVar.SERVER);
}
