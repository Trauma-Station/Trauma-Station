using System.Linq;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Trauma.Common.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Trauma.Client.ClockworkCult.Stylesheets;

/// <summary>
/// Stylesheet used for Clockwork Cult UIs
/// </summary>
[LoadStylesheet]
public sealed partial class ClockworkStylesheet : CommonStylesheet
{
    public override string StylesheetName => "Clockwork";

    public override NotoFontFamilyStack BaseFont { get; }

    public VectorFont ClockworkFontSmall { get; }
    public VectorFont ClockworkFontNormal { get; }
    public VectorFont ClockworkFontLarge { get; }

    /// <summary>
    /// The background texture of the window
    /// </summary>
    public Texture SlabBackgroundTexture { get; }

    public static readonly ResPath TextureRoot = new("/Textures/_Trauma/Interface/Alien");

    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        { typeof(TextureResource), [TextureRoot] },
    };

    private const int PrimaryFontSize = 16;
    private const int FontSizeStep = 2;

    private readonly List<(string?, int)> _commonFontSizes = new()
    {
        (null, PrimaryFontSize),
        (StyleClass.FontSmall, PrimaryFontSize - FontSizeStep),
        (StyleClass.FontLarge, PrimaryFontSize + FontSizeStep),
    };

    public ClockworkStylesheet(object config, StylesheetManager man) : base(config)
    {
        BaseFont = new NotoFontFamilyStack(ResCache);

        // RAT'VAR PLEASE BLESS ME WITH DOCUMENTATION IN UI
        var fontRes = ResCache.GetResource<FontResource>("/Fonts/_Trauma/JimNightshade-Regular.ttf");

        SlabBackgroundTexture = ResCache.GetResource<TextureResource>("/Textures/Interface/Paper/paper_background_blood_red.svg.96dpi.png");

        ClockworkFontSmall = new VectorFont(fontRes, PrimaryFontSize - FontSizeStep);
        ClockworkFontNormal = new VectorFont(fontRes, PrimaryFontSize);
        ClockworkFontLarge = new VectorFont(fontRes, PrimaryFontSize + FontSizeStep);

        var rules = new[]
        {
            GetRulesForFont(null, BaseFont, _commonFontSizes),
            [
                Element().Prop(Label.StylePropertyFont, ClockworkFontNormal),
                Element().Class(StyleClass.FontSmall).Prop(Label.StylePropertyFont, ClockworkFontSmall),
                Element().Class(StyleClass.FontLarge).Prop(Label.StylePropertyFont, ClockworkFontLarge),
            ],

            GetAllSheetletRules<PalettedStylesheet, CommonSheetletAttribute>(man),
            GetAllSheetletRules<ClockworkStylesheet, CommonSheetletAttribute>(man),
        };

        Stylesheet = new Stylesheet(rules.SelectMany(x => x).ToArray());
    }
}
