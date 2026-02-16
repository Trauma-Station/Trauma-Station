// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Added to entities scanned by the genetics console.
/// This allows mutations to be discovered, activated and stored to disks for further printing.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ScannedGenomeSystem))]
public sealed partial class ScannedGenomeComponent : Component
{
    /// <summary>
    /// The sequences this mob can have mutated.
    /// Not networked, the client has to get them via BUI SequenceState.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<Sequence> Sequences = new();
}

/// <summary>
/// A possible mutation which can be sequenced to unlock a mutation.
/// It will also activate it in the scanned mob, after which it is discovered globally.
/// </summary>
[DataRecord]
public sealed partial class Sequence
{
    public EntProtoId<MutationComponent> Mutation;

    public string Bases = string.Empty;
}

[DataRecord]
public partial record struct UnknownBase(uint Index, char Value = 'X');

// EntProtoId doesnt work properly with Serializable for some reason
[Serializable, NetSerializable]
public record struct SequenceState(string Bases, int Number, string? Mutation);
