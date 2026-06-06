// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Reagents;

/// <summary>
/// Prototype for <see cref="ReagentPrototype"/>, in order to classify the reagent into a specific group.
/// </summary>
[Prototype]
public sealed partial class ReagentGroupPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
}
