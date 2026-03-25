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
        // Original image palette - exact colors
        var bgColor      = new Color(0.07f, 0.07f, 0.07f, 1.00f); // near-black / dark gray
        var textColor    = new Color(0.00f, 1.00f, 0.00f, 1.00f); // bright lime green #00FF00
        var borderColor  = new Color(0.00f, 0.90f, 0.00f, 1.00f); // slightly softer green for borders
        var buttonBg     = new Color(0.07f, 0.07f, 0.07f, 1.00f);
        var hoverColor   = new Color(0.15f, 0.15f, 0.15f, 1.00f);
        var pressedColor = new Color(0.25f, 0.25f, 0.25f, 1.00f);
        var warningColor = new Color(1.00f, 0.65f, 0.00f, 1.00f); // warning orange

        var asciiBorderBox = new StyleBoxFlat
        {
            BackgroundColor = bgColor,
            BorderColor = borderColor,
            BorderThickness = new Thickness(3f),
            Padding = new Thickness(6f)
        };

        var buttonBox = new StyleBoxFlat
        {
            BackgroundColor = buttonBg,
            BorderColor = borderColor,
            BorderThickness = new Thickness(2f),
            Padding = new Thickness(8f, 4f)
        };

        return
        [
            // Main Window (exact CRT dark frame + lime text)
            E()
                .Class(DefaultWindow.StyleClassWindowPanel)
                .Panel(asciiBorderBox),

            // Window title bar (uppercase green, matches original image)
            E<Label>()
                .Class("FancyWindowTitle") // hardcoded award
                .Prop(Label.StylePropertyFontColor, textColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(16)),

            // Panels (Captured Humans / Available Experiments)
            E<PanelContainer>()
                .Panel(asciiBorderBox),

            // All Labels (main text)
            E<Label>()
                .Prop(Label.StylePropertyFontColor, textColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(13)),

            // Buttons ([SELECT], [PERFORM SELECTED], etc.)
            E<ContainerButton>()
                .Prop(ContainerButton.StylePropertyStyleBox, buttonBox),

            // Button hover (classic green flash)
            E<ContainerButton>()
                .PseudoHovered()
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                {
                    BackgroundColor = hoverColor,
                    BorderColor = borderColor,
                    BorderThickness = new Thickness(2f)
                }),

            // Button pressed
            E<ContainerButton>()
                .PseudoPressed()
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                {
                    BackgroundColor = pressedColor,
                    BorderColor = textColor,
                    BorderThickness = new Thickness(2f)
                }),

            // Warning text at the bottom (orange)
            E<Label>()
                .Class("negative")
                .Prop(Label.StylePropertyFontColor, warningColor)
        ];
    }
}
