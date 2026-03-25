// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Prototypes;

[Prototype]
public sealed partial class HereticKnowledgePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public HereticPath? Path;

    [DataField]
    public int Stage = 1;

    /// <summary>
    ///     Indicates that this should not be on a main branch.
    /// </summary>
    [DataField]
    public bool SideKnowledge;

    /// <summary>
    ///     What knowledge event should be raised (on body)
    /// </summary>
    [DataField, NonSerialized]
    public HereticKnowledgeEvent? Event;

    /// <summary>
    ///     What event should be raised (on mind)
    /// </summary>
    [DataField]
    public object? MindEvent;

    /// <summary>
    ///     What rituals should be given
    /// </summary>
    [DataField]
    public List<EntProtoId<Rituals.HereticRitualComponent>>? RitualPrototypes;

    /// <summary>
    ///     What actions should be given
    /// </summary>
    [DataField] public List<EntProtoId>? ActionPrototypes;

    /// <summary>
    ///     Used for codex
    /// </summary>
    [DataField] public string LocName = string.Empty;

    /// <summary>
    ///     Used for codex
    /// </summary>
    [DataField] public string LocDesc = string.Empty;
}
