// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets.Palette;

namespace Content.Trauma.Client.Stylesheets;

public sealed partial class AlienStylesheet
{
    public override ColorPalette PrimaryPalette => Palettes.Green;
    public override ColorPalette SecondaryPalette => Palettes.Slate;
    public override ColorPalette PositivePalette => Palettes.Green;
    public override ColorPalette NegativePalette => Palettes.Gold;
    public override ColorPalette HighlightPalette => Palettes.Red;
}
