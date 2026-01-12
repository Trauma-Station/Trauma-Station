namespace Content.Shared.Humanoid.Markings;

public static class TraumaMarkingCategories
{
    public static bool IgnoresMatchSkin(this MarkingCategories category)
    {
        return category switch
        {
            MarkingCategories.HairSpecial => true,
            MarkingCategories.FacialHairSpecial => true,
            _ => false
        };
    }
}
