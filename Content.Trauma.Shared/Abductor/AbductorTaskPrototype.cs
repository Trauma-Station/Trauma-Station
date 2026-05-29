// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.Abductor;

/// <summary>
/// A possible task to pick for an experiment.
/// The subject has to meet some conditions for it to be picked, then once it is picked it has conditions to check if it was completed.
/// </summary>
[Prototype]
public sealed partial class AbductorTaskPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Get the localized name for this task.
    /// </summary>
    [ViewVariables]
    public string Name => Loc.GetString("abductor-task-" + ID);

    /// <summary>
    /// Chance for this task to be valid before anything else.
    /// </summary>
    [DataField]
    public float Chance = 1f;

    /// <summary>
    /// Conditions the subject has to meet for this task to be considered.
    /// </summary>
    [DataField]
    public EntityCondition[]? Valid;

    /// <summary>
    /// Conditions the subject has to meet for this task to be completed.
    /// If this is null, the inverse of <see cref="Valid"/> will be used.
    /// </summary>
    [DataField]
    public EntityCondition[]? Completed;

    /// <summary>
    /// Whether this task can be picked randomly.
    /// </summary>
    [DataField]
    public bool Random = true;
}
