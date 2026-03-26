// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Abductor;

/// <summary>
/// Stores tasks of a subject being experimented on by abductors.
/// Tasks must be completed in the order they are given.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AbductorTaskSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class AbductorSubjectComponent : Component
{
    /// <summary>
    /// Every picked task that must be completed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<AbductorTaskPrototype>> Tasks = new();

    /// <summary>
    /// How many tasks have been completed.
    /// Tasks must be completed in order
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CompletedCount;

    /// <summary>
    /// Whether all tasks are completed or not.
    /// </summary>
    [ViewVariables]
    public bool AllCompleted => CompletedCount >= Tasks.Count;

    /// <summary>
    /// The next task that must be completed.
    /// </summary>
    [ViewVariables]
    public ProtoId<AbductorTaskPrototype> NextTask => AllCompleted ? string.Empty : Tasks[CompletedCount];
}
