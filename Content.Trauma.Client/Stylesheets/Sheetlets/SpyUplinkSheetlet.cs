// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Trauma.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class SpyUplinkSheetlet : Sheetlet<SpyUplinkStylesheet>
{
    public override StyleRule[] GetRules(SpyUplinkStylesheet sheet, object config)
    {
        var red = Color.FromHex("#DC2323");
        var green = Color.FromHex("#5BA626");
        var yellow = Color.FromHex("#F3890C");

        var transparentBox = new StyleBoxFlat
        {
            BackgroundColor = sheet.SecondaryPalette.BackgroundDark.WithAlpha(0.7f),
        };

        var lightPanel = new StyleBoxFlat
        {
            BackgroundColor = sheet.PrimaryPalette.Base,
        };

        return
        [
            E<Label>()
                .Class("SpyBountyEasy")
                .Prop(Label.StylePropertyFontColor, green)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12, FontKind.Italic)),

            E<Label>()
                .Class("SpyBountyMedium")
                .Prop(Label.StylePropertyFontColor, yellow)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12, FontKind.Italic)),

            E<Label>()
                .Class("SpyBountyHard")
                .Prop(Label.StylePropertyFontColor, red)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12, FontKind.Italic)),

            E()
                .Class("SpyBountyClaimed")
                .AlignMode(Label.AlignMode.Center)
                .Prop(Label.StylePropertyFontColor, red)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(16, FontKind.Italic))
                .Panel(transparentBox),

            E<PanelContainer>()
                .Class(StyleClass.PanelLight)
                .Panel(lightPanel),
        ];
    }
}
