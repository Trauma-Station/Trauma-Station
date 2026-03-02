// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Changeling;

/// <summary>
/// Holds the necessary information about the starting evolutions of a changeling.
/// </summary>
[Prototype]
public sealed partial class ChangelingStartingEvolutionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The components that the changeling starts with on MapInit
    /// </summary>
    [DataField]
    public ComponentRegistry Components { get; private set; } = default!;
}
