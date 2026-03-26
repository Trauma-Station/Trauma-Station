// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Trauma.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class AlienSheetlet : Sheetlet<AlienStylesheet>
{
    public override StyleRule[] GetRules(AlienStylesheet sheet, object config)
    {
        var bgColor      = new Color(37, 31, 27);
        var textColor    = new Color(0, 255, 0);
        var borderColor  = new Color(212, 0, 98);
        var buttonBg     = new Color(60, 165, 68);
        var buttonBorder = new Color(55, 142, 64);
        var hoverColor   = new Color(50, 128, 108);
        var pressedColor = borderColor;
        var warningColor = new Color(1f, 0.65f, 0f);

        var asciiBorderBox = new StyleBoxFlat
        {
            BackgroundColor = bgColor,
            BorderColor = borderColor,
            BorderThickness = new Thickness(3f)
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
                .Panel(asciiBorderBox),

            // Window title bar
            E<Label>()
                .Class("FancyWindowTitle") // hardcoded award
                .AlignMode(Label.AlignMode.Center)
                .Prop(Label.StylePropertyFontColor, textColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(16)),

            // Other panels
            E<PanelContainer>()
                .Panel(asciiBorderBox),

            // All Labels
            E<Label>()
                .Prop(Label.StylePropertyFontColor, textColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(13)),

            // Buttons
            E<ContainerButton>()
                .PseudoNormal()
                .ParentOf(E<PanelContainer>())
                .Panel(buttonBox),

            // Button hover
            E<ContainerButton>().PseudoHovered()
                .ParentOf(E<PanelContainer>())
                .Panel(new StyleBoxFlat
                {
                    BackgroundColor = hoverColor,
                    BorderColor = buttonBorder,
                    BorderThickness = new Thickness(2f),
                    Padding = new Thickness(8f, 4f)
                }),

            E<ContainerButton>()
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
            E<ContainerButton>()
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
