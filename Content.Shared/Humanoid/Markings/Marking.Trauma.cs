namespace Content.Shared.Humanoid.Markings;

/// <summary>
/// Trauma - implement ToString bruh
/// </summary>
public partial record struct Marking
{
    public override string ToString()
        => ToLegacyDbString();
}
