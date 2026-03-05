// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Knowledge;

/// <summary>
/// A knowledge profile for a species.
/// This is the base of any character's skills, the humanoid profile can then tweak it.
/// </summary>
[Prototype]
public sealed partial class KnowledgeProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [IncludeDataField]
    public KnowledgeProfile Profile;

    /// <summary>
    /// How many points you can spend on skills.
    /// Must at least be enough for <see cref="Profile"/>.
    /// Unused points are wasted.
    /// </summary>
    // TODO: write a test that this always holds.
    [DataField(required: true)]
    public int Points;
}
