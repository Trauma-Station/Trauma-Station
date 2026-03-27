// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Trauma.Client.ClockworkCult.Stylesheets.Sheetlet;

[CommonSheetlet]
public sealed class ClockworkSheetlet : Sheetlet<ClockworkStylesheet>
{
    public override StyleRule[] GetRules(ClockworkStylesheet sheet, object config)
    {
        var bgColor      = new Color(207, 223, 0);
        var textColor    = new Color(240, 240, 240);
        var borderColor  = new Color(153, 101, 21);
        var buttonBg     = new Color(128, 0, 0);
        var buttonBorder = new Color(140, 0, 0);
        var hoverColor   = new Color(165, 102, 57);
        var pressedColor = borderColor;
        var warningColor = new Color(1f, 0.65f, 0f);

        var clockworkBox = new StyleBoxFlat()
        {
            BackgroundColor = bgColor,
            BorderColor = borderColor,
        };

        var buttonBox = new StyleBoxFlat
        {
            BackgroundColor = buttonBg,
            BorderColor = buttonBorder,
            BorderThickness = new Thickness(2f),
            Padding = new Thickness(8f, 4f)
        };

        return
        [
            // Window background panel
            E()
                .Class(StyleClass.BackgroundPanel)
                .Panel(clockworkBox),

            // Window title bar
            E<Label>()
                .Class("FancyWindowTitle")
                .AlignMode(Label.AlignMode.Center)
                .Prop(Label.StylePropertyFontColor, textColor)
                .Prop(Label.StylePropertyFont, sheet.ClockworkFontLarge),

            // All Labels
            E<Label>()
                .Prop(Label.StylePropertyFontColor, textColor)
                .Prop(Label.StylePropertyFont, sheet.ClockworkFontNormal),

            // Buttons
            E<Button>()
                .PseudoNormal()
                .ParentOf(E<PanelContainer>())
                .Panel(buttonBox),

            // Button hover
            E<Button>()
                .PseudoHovered()
                .ParentOf(E<PanelContainer>())
                .Panel(new StyleBoxFlat
                {
                    BackgroundColor = hoverColor,
                    BorderColor = buttonBorder,
                    BorderThickness = new Thickness(2f),
                    Padding = new Thickness(8f, 4f)
                }),

            E<Button>()
                .Class("highlight")
                .ParentOf(E<PanelContainer>())
                .Panel(new StyleBoxFlat
                {
                    BackgroundColor = hoverColor,
                    BorderColor = buttonBorder,
                    BorderThickness = new Thickness(2f),
                    Padding = new Thickness(8f, 4f)
                }),

            // Button pressed
            E<Button>()
                .PseudoPressed()
                .ParentOf(E<PanelContainer>())
                .Panel(new StyleBoxFlat
                {
                    BackgroundColor = pressedColor,
                    BorderColor = textColor,
                    BorderThickness = new Thickness(2f),
                    Padding = new Thickness(8f, 4f)
                }),

            // Caution labels
            E<Label>()
                .Class("negative")
                .Prop(Label.StylePropertyFontColor, warningColor)
        ];
    }
}
