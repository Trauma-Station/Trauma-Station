using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class DnaData : ReagentData
{
    [DataField]
    public string DNA = string.Empty;

    /// <summary>
    /// Goobstation - time this DNA was taken at, shown by forensic scanner.
    /// </summary>
    [DataField]
    public TimeSpan Freshness = TimeSpan.Zero;

    public override ReagentData Clone()
    {
        return new DnaData
        {
            DNA = DNA,
            Freshness = Freshness, // Goob
        };
    }

    public override bool Equals(ReagentData? other)
    {
        if (other == null)
        {
            return false;
        }

        return ((DnaData) other).DNA == DNA;
        // Trauma note - Freshness is intentionally not checked as it would make the same persons blood not combine
    }

    public override int GetHashCode()
    {
        return DNA.GetHashCode();
    }
}
