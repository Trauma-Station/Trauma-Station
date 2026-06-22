// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets.Palette;

namespace Content.Trauma.Client.Stylesheets;

public sealed partial class SpyUplinkStylesheet
{
    public override ColorPalette PrimaryPalette => Palettes.Cyan;
    public override ColorPalette SecondaryPalette => ColorPalette.FromHexBase("#333333");
    public override ColorPalette PositivePalette => Palettes.Green;
    public override ColorPalette NegativePalette => Palettes.Red;
    public override ColorPalette HighlightPalette => Palettes.Amber;
}
