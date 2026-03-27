using Content.Client.Stylesheets.Palette;

namespace Content.Trauma.Client.ClockworkCult.Stylesheets;

public sealed partial class ClockworkStylesheet
{
    public override ColorPalette PrimaryPalette => Palettes.Gold;
    public override ColorPalette SecondaryPalette => ColorPalette.FromHexBase("#4A2E15");
    public override ColorPalette PositivePalette => Palettes.Green;
    public override ColorPalette NegativePalette => Palettes.Red;
    public override ColorPalette HighlightPalette => ColorPalette.FromHexBase("#C5A059");
}
