using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Trauma.Common.Stylesheets;
using Robust.Client.ResourceManagement;
using System.Linq;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Trauma.Client.Stylesheets;

[LoadStylesheet]
public sealed partial class TraumaStylesheet : CommonStylesheet
{
    public override string StylesheetName => "Trauma";

    public override NotoFontFamilyStack BaseFont { get; }

    public static readonly ResPath TextureRoot = new("/Textures/_Trauma/Interface/Trauma");

    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        { typeof(TextureResource), [TextureRoot] },
    };

    private const int PrimaryFontSize = 12;
    private const int FontSizeStep = 2;

    private readonly List<(string?, int)> _commonFontSizes = new()
    {
        (null, PrimaryFontSize),
        (StyleClass.FontSmall, PrimaryFontSize - FontSizeStep),
        (StyleClass.FontLarge, PrimaryFontSize + FontSizeStep),
    };

    public TraumaStylesheet(object config, StylesheetManager man) : base(config)
    {
        BaseFont = new NotoFontFamilyStack(ResCache);
        var rules = new[]
        {
            // Set up important rules that need to go first.
            GetRulesForFont(null, BaseFont, _commonFontSizes),
            // Set up our core rules.
            [
                // Declare the default font.
                Element().Prop(Label.StylePropertyFont, BaseFont.GetFont(PrimaryFontSize)),
            ],
            // Finally, load all the other sheetlets.
            GetAllSheetletRules<PalettedStylesheet, CommonSheetletAttribute>(man),
            GetAllSheetletRules<TraumaStylesheet, CommonSheetletAttribute>(man),
        };

        Stylesheet = new Stylesheet(rules.SelectMany(x => x).ToArray());
    }
}
