namespace Content.Shared.Forensics.Components;

public sealed partial class ForensicScannerComponent
{
    [ViewVariables, AutoNetworkedField]
    public List<(string, TimeSpan)> SolutionDNAs = new();
}
