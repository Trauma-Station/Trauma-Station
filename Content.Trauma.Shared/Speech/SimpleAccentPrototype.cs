// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Dataset;
using Content.Shared.Speech.Prototypes;

namespace Content.Trauma.Shared.Speech;

/// <summary>
/// A simple accent based on a base replacement accent with prefix and suffix datasets.
/// </summary>
[Prototype]
public sealed partial class SimpleAccentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Base replacement accent to apply first.
    /// </summary>
    [DataField]
    public ProtoId<ReplacementAccentPrototype>? Replacement;

    /// <summary>
    /// Dataset to pick from and prepend to the start of the message.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Prefix;

    /// <summary>
    /// Chance of rolling <see cref="Prefix"/> if used.
    /// </summary>
    [DataField]
    public float PrefixChance;

    /// <summary>
    /// Dataset to pick from and append to the end of the message.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Suffix;

    /// <summary>
    /// Chance of rolling <see cref="Suffix"/> if used.
    /// </summary>
    [DataField]
    public float SuffixChance = 1f;

    /// <summary>
    /// Whether to make the final message all caps.
    /// </summary>
    [DataField]
    public bool Uppercase;
}
