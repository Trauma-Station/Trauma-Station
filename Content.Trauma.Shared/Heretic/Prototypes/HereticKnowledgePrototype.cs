// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Rituals;

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
    public List<EntProtoId<HereticRitualComponent>>? RitualPrototypes;

    /// <summary>
    ///     What actions should be given
    /// </summary>
    [DataField]
    public List<EntProtoId>? ActionPrototypes;

    /// <summary>
    /// Sets passive ability level this value, if it is greater than current one
    /// </summary>
    /// <remarks>Could use <see cref="MindEvent"/> for this, but it would require making a list of mind events
    /// which would look ugly in yaml</remarks>
    [DataField]
    public int PassiveLevel;
}
