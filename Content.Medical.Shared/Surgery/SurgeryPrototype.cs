// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Wounds;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Surgery;

/// <summary>
/// Specifies all neccessary data for surgeries.
/// </summary>
[Prototype]
public sealed partial class SurgeryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Tool required for surgery.
    /// </summary>
    [DataField]
    public ComponentRegistry? Tool;

    /// <summary>
    /// Effects caused by surgery.
    /// </summary>
    [DataField]
    public EntityEffect[]? SurgeryEffects;

    /// <summary>
    /// How long surgery takes
    /// </summary>
    [DataField]
    public float Duration = 2f;

    /// <summary>
    /// Wounds required on target part to perform surgery.
    /// </summary>
    [DataField]
    public List<EntProtoId>? Required;

    /// <summary>
    /// Wounds that block surgery (e.g., OpenIncision).
    /// </summary>
    [DataField]
    public List<EntProtoId>? Forbidden;

    /// <summary>
    /// Auto repeat surgery after completion if possible.
    /// </summary>
    [DataField]
    public bool Repeat = false;

    /// <summary>
    /// Surgery complexity modifier. Higher value means more complex.
    /// </summary>
    [DataField]
    public int Complexity = 0;
}
