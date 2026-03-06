// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Knowledge;

/// <summary>
/// A knowledge profile for a species.
/// This is the base of any character's skills, the humanoid profile can then tweak it.
/// The point cost of this profile is used as the points limit which players can work with.
/// </summary>
[Prototype]
public sealed partial class KnowledgeProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [IncludeDataField]
    public KnowledgeProfile Profile;
}
