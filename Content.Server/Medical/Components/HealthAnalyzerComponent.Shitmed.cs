using Content.Trauma.Common.Medical.HealthAnalyzer;

namespace Content.Server.Medical.Components;

public sealed partial class HealthAnalyzerComponent
{
    /// <summary>
    /// Shitmed Change: The current mode of the scanner.
    /// </summary>
    [DataField]
    public HealthAnalyzerMode CurrentMode = HealthAnalyzerMode.Body;
}
