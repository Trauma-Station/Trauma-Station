// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Console;

/// <summary>
/// Links a clicked mob to a <see cref="GeneticsScannerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GeneticsConsoleSystem))]
public sealed partial class HandheldGeneticsScannerComponent : Component
{
    /// <summary>
    /// How long the mob linking doafter is.
    /// </summary>
    [DataField]
    public TimeSpan LinkTime = TimeSpan.FromSeconds(3);
}
