// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Random;
using System.Text;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Data every mutation prototype has, changes every round.
/// </summary>
[DataRecord]
public sealed partial class MutationData
{
    public static readonly char[] AGCT = new[] { 'A', 'G', 'C', 'T' };
    public const int PairCount = 16;
    public const int BaseCount = PairCount * 2;

    /// <summary>
    /// Top base characters with the bottom base characters concatenated after them.
    /// </summary>
    public string Bases = string.Empty;

    /// <summary>
    /// Once a mutation is discovered via activation in the genetics console,
    /// it will always be recognized when scanned in the future.
    /// </summary>
    public bool Discovered;

    /// <summary>
    /// Mutation number assigned at roundstart.
    /// This is unique to each mutation so you can identify which ones are the
    /// same across different subjects before you discover the actual mutation.
    /// </summary>
    public int Number;

    public void Scramble(IRobustRandom random, int number)
    {
        var builder = new StringBuilder();
        for (int p = 0; p < PairCount; p++)
        {
            builder.Append(random.Pick(AGCT));
        }
        for (int p = 0; p < PairCount; p++) {
            builder.Append(GetMatching(builder[p]));
        }
        Bases = builder.ToString();
        Number = number;
    }

    /// <summary>
    /// Get the matching base for a given base.
    /// </summary>
    public static char GetMatching(char b)
        => b switch
        {
            'A' => 'G',
            'G' => 'A',
            'C' => 'T',
            'T' => 'C',
            _ => 'X'
        };
}
