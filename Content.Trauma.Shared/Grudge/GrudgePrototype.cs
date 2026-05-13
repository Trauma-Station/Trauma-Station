// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;

namespace Content.Trauma.Shared.Grudges;

/// <summary>
/// Prototypes for choosing grudges
/// </summary>
[Prototype]
public sealed partial class GrudgePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public EntProtoId AccuserObjective;

    [DataField]
    public EntProtoId AccusedObjective;

    [DataField]
    public List<ProtoId<SpeciesPrototype>>? AllowedAccuserSpecies;

    [DataField]
    public bool InvertAccuserSpecies;

    [DataField]
    public List<ProtoId<SpeciesPrototype>>? AllowedAccusedSpecies;

    [DataField]
    public bool InvertAccusedSpecies;

    [DataField]
    public List<ProtoId<JobPrototype>>? AllowedAccuserJob;

    [DataField]
    public bool InvertAccuserJob;

    [DataField]
    public List<ProtoId<JobPrototype>>? AllowedAccusedJob;

    [DataField]
    public bool InvertAccusedJob;
}
