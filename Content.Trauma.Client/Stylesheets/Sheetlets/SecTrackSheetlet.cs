// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Trauma.Client.SecTrack.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Trauma.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class SecTrackSheetlet : Sheetlet<SecTrackStylesheet>
{
    public const string StyleClassConsoleHeading = "SecTrackConsoleHeading";
    public const string StyleClassButtonRed = "SecTrackButtonRed";
    public const string StyleClassConsoleLineEdit = "SecTrackConsoleLineEdit";
    public const string StyleClassOptionButton = "SecTrackOptionButton";

    public const string StyleClassSquadMemberAlive = "SecTrackSquadMemberAlive";
    public const string StyleClassSquadMemberDead = "SecTrackSquadMemberDead";

    public const string StyleClassConsoleSubText = "SecTrackConsoleSubText";
    public const string StyleClassConsoleSubTextOne = "SecTrackConsoleSubTextOne";
    public const string StyleClassConsoleSubTextTwo = "SecTrackConsoleSubTextTwo";

    public const string StyleClassMemberTextDead = "SecTrackMemberTextDead";

    public const string StyleClassTimerHeader = "SecTrackTimerHeader";
    public const string StyleClassTimerText = "SecTrackTimerText";

    public const string StyleClassTimerNormal = "SecTrackTimerNormal";
    public const string StyleClassTimerWarning = "SecTrackTimerWarning";
    public const string StyleClassTimerCritical = "SecTrackTimerCritical";
    public const string StyleClassTimerOverdue = "SecTrackTimerOverdue";

    public const string StyleClassTabContainer = "SecTrackTabContainer";
    //public const string StyleClassPanelEntry = "SecTrackPanelEntry";
    //public const string StyleClassSquadEntryPanel = "SecTrackSquadEntryPanel";
    //public const string StyleClassSquadMemberAlive = "SecTrackSquadMemberAlive";
    //public const string StyleClassSquadMemberDead = "SecTrackSquadMemberDead";
    //public const string StyleClassTimerPanel = "SecTrackTimerPanel";
    //public const string StyleClassTimerTextHeader = "SecTrackTimerTextHeader";
    //public const string StyleClassTimerTextSub = "SecTrackTimerTextSub";

    public static Color TabActiveColor => Color.FromHex("#ff4444");
    public static Color TabInactiveColor => Color.FromHex("#ff8888");
    public static Color TextColor => Color.FromHex("#ff9999");
    public static Color PlaceholderColor => Color.FromHex("#ff6666");

    public override StyleRule[] GetRules(SecTrackStylesheet sheet, object config)
    {
        var tabActiveStyle = CreateStyleBox(
            Color.FromHex("#440000"),
            TabActiveColor,
            new Thickness(2, 2, 2, 0),
            new Thickness(10, 5, 10, 5));

        var tabInactiveStyle = CreateStyleBox(
            Color.FromHex("#220000"),
            TabInactiveColor,
            new Thickness(2, 2, 2, 0),
            new Thickness(10, 5, 10, 5));

        var panelStyle = CreateStyleBox(
            Color.FromHex("#110000"),
            TabActiveColor,
            new Thickness(2f),
            new Thickness(5f));

        var buttonRedStyle = CreateStyleBox(
            Color.FromHex("#660000"),
            TabActiveColor,
            new Thickness(1f),
            new Thickness(8f, 4f, 8f, 4f));

        var lineEditStyle = CreateStyleBox(
            Color.FromHex("#110000"),
            TabActiveColor,
            new Thickness(1f),
            new Thickness(4f, 2f, 4f, 2f));

        var optionButtonStyle = CreateStyleBox(
            Color.FromHex("#330000"),
            TabActiveColor,
            new Thickness(1),
            new Thickness(6, 3, 6, 3)
        );

        return
        [
            // TabContainer
            E<TabContainer>()
            .Class(StyleClassTabContainer)
                .Prop(TabContainer.StylePropertyTabStyleBox, tabActiveStyle)
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, tabInactiveStyle)
                .Prop(TabContainer.StylePropertyPanelStyleBox, panelStyle)
                .Prop(TabContainer.stylePropertyTabFontColor, TabActiveColor)
                .Prop(TabContainer.StylePropertyTabFontColorInactive, TabInactiveColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12, FontKind.Bold)),

            // Labels
            E<Label>()
                .Class(StyleClassConsoleHeading)
                .Prop(Label.StylePropertyFontColor, TabActiveColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(16, FontKind.Bold)),

            E<Label>()
                .Class(StyleClassConsoleSubText)
                .Prop(Label.StylePropertyFontColor, TextColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12)),

            E<Label>()
                .Class(StyleClassConsoleSubTextOne)
                .Prop(Label.StylePropertyFontColor, TextColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(13)),

            E<Label>()
                .Class(StyleClassConsoleSubTextTwo)
                .Prop(Label.StylePropertyFontColor, TabInactiveColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(11)),

            E<Label>()
                .Class(StyleClassMemberTextDead)
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#888888"))
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12)),

            E<Label>()
                .Class(StyleClassTimerHeader)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12)),

            E<Label>()
                .Class(StyleClassTimerText)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(10)),

            E<Label>()
                .Class(StyleClassTimerNormal)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12))
                .Prop(Label.StylePropertyFontColor, PlaceholderColor),

            E<Label>()
                .Class(StyleClassTimerWarning)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#ff9933")),

            E<Label>()
                .Class(StyleClassTimerCritical)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#ff3333")),

            E<Label>()
                .Class(StyleClassTimerOverdue)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#ff0000")),

            // Buttons
            E<Button>()
                .Class(StyleClassButtonRed)
                .Prop(Button.StylePropertyStyleBox, buttonRedStyle)
                .Prop(Label.StylePropertyFontColor, TextColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12)),

            // LineEdit
            E<LineEdit>()
                .Class(StyleClassConsoleLineEdit)
                .Prop(LineEdit.StylePropertyStyleBox, lineEditStyle)
                .Prop(Label.StylePropertyFontColor, TextColor)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12))
                .Prop(LineEdit.StylePropertyCursorColor, TabActiveColor)
                .Prop(LineEdit.StylePropertySelectionColor, TabActiveColor.WithAlpha(0.3f)),

            // OptionButton
            E<OptionButton>()
                .Class(StyleClassOptionButton)
                .Prop(ContainerButton.StylePropertyStyleBox, optionButtonStyle)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12))
                .Prop(Label.StylePropertyFontColor, TextColor),

            // OptionButton dropdown background
            E<PanelContainer>()
                .Class(OptionButton.StyleClassOptionsBackground)
                .Panel(new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#330000"),
                    BorderColor = TabActiveColor,
                    BorderThickness = new Thickness(1)
                }),

            E<PanelContainer>()
                .Class(StyleClassSquadMemberAlive)
                .Panel(new StyleBoxFlat {
                    BackgroundColor = Color.FromHex("#3a0f0f"),
                    BorderColor = PlaceholderColor,
                    BorderThickness = new Thickness(1f),
                    Padding = new Thickness(8f, 4f)
                }),

            E<PanelContainer>()
                .Class(StyleClassSquadMemberDead)
                .Panel(new StyleBoxFlat {
                    BackgroundColor = Color.FromHex("#1a0a0a"),
                    BorderColor = Color.FromHex("#990000"),
                    BorderThickness = new Thickness(1f),
                    Padding = new Thickness(8f, 4f)
                }),
        ];
    }
    private StyleBoxFlat CreateStyleBox(Color backgroundColor, Color borderColor,
        Thickness borderThickness, Thickness? contentMargin = null)
    {
        var style = new StyleBoxFlat
        {
            BackgroundColor = backgroundColor,
            BorderColor = borderColor,
            BorderThickness = borderThickness
        };

        if (contentMargin.HasValue)
        {
            style.ContentMarginLeftOverride = contentMargin.Value.Left;
            style.ContentMarginRightOverride = contentMargin.Value.Right;
            style.ContentMarginTopOverride = contentMargin.Value.Top;
            style.ContentMarginBottomOverride = contentMargin.Value.Bottom;
        }

        return style;
    }
}
