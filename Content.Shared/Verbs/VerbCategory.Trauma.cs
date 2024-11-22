using Robust.Shared.Utility;

namespace Content.Shared.Verbs;

/// <summary>
/// Trauma - extra constructor for RSI icons
/// </summary>
public sealed partial class VerbCategory
{
    public VerbCategory(LocId text, SpriteSpecifier icon, bool iconsOnly = false)
    {
        Text = Loc.GetString(text);
        Icon = icon;
        IconsOnly = iconsOnly;
    }
}
