// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._EinsteinEngines.Language.Components;

/// <summary>
/// Assigned to the knowledge entity that holds information about what languages the parent knows.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LanguageKnowledgeComponent : Component
{
    /// <summary>
    ///     Can this entity speak without any external tools.
    /// </summary>
    [DataField]
    public bool Speaks = true;

    /// <summary>
    ///     Can this entity this entity understand without any external tools.
    /// </summary>
    [DataField]
    public bool Understands = true;

    /// <summary>
    ///     Id of the language this knowledge represents.
    /// </summary>
    [DataField]
    public ProtoId<LanguagePrototype> LanguageId;

    /// <summary>
    ///     Time of last sent message.
    /// </summary>
    [DataField]
    public uint LastSentMessage = 0;
}
