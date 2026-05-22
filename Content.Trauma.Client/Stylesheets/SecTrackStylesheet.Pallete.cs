// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets.Palette;

namespace Content.Trauma.Client.SecTrack.Stylesheets;

public sealed partial class SecTrackStylesheet
{
    public override ColorPalette PrimaryPalette => Palettes.Red;
    public override ColorPalette SecondaryPalette => Palettes.Slate;
    public override ColorPalette PositivePalette => Palettes.Red;
    public override ColorPalette NegativePalette => Palettes.Cyan;
    public override ColorPalette HighlightPalette => Palettes.Green;
}
