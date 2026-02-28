// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Adds an offset to cold and/or heat damage thresholds.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TemperatureDamageMutationSystem))]
public sealed partial class TemperatureDamageMutationComponent : Component
{
    [DataField]
    public float ColdOffset;

    [DataField]
    public float HeatOffset;
}
