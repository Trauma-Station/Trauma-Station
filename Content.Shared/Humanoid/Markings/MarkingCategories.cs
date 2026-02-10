using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Markings
{
    [Serializable, NetSerializable]
    public enum MarkingCategories : byte
    {
        // <Trauma>
        HairSpecial,
        FacialHairSpecial,
        Face,
        RightArm,
        RightHand,
        LeftArm,
        LeftHand,
        RightLeg,
        RightFoot,
        LeftLeg,
        LeftFoot,
        Groin,
        Wings,
        // </Trauma>
        Special,
        Hair,
        FacialHair,
        Head,
        HeadTop,
        HeadSide,
        Snout,
        SnoutCover,
        Chest,
        UndergarmentTop,
        UndergarmentBottom,
        Arms,
        Legs,
        Tail,
        Overlay
    }

    public static class MarkingCategoriesConversion
    {
        public static MarkingCategories FromHumanoidVisualLayers(HumanoidVisualLayers layer)
        {
            return layer switch
            {
                // <Trauma>
                HumanoidVisualLayers.HairSpecial => MarkingCategories.HairSpecial,
                HumanoidVisualLayers.FacialHairSpecial => MarkingCategories.FacialHairSpecial,
                HumanoidVisualLayers.Face => MarkingCategories.Face,
                HumanoidVisualLayers.Groin => MarkingCategories.Groin,
                HumanoidVisualLayers.Wings => MarkingCategories.Wings,
                // </Trauma>
                HumanoidVisualLayers.Special => MarkingCategories.Special,
                HumanoidVisualLayers.Hair => MarkingCategories.Hair,
                HumanoidVisualLayers.FacialHair => MarkingCategories.FacialHair,
                HumanoidVisualLayers.Head => MarkingCategories.Head,
                HumanoidVisualLayers.HeadTop => MarkingCategories.HeadTop,
                HumanoidVisualLayers.HeadSide => MarkingCategories.HeadSide,
                HumanoidVisualLayers.Snout => MarkingCategories.Snout,
                HumanoidVisualLayers.Chest => MarkingCategories.Chest,
                HumanoidVisualLayers.UndergarmentTop => MarkingCategories.UndergarmentTop,
                HumanoidVisualLayers.UndergarmentBottom => MarkingCategories.UndergarmentBottom,
                // <Trauma> - distinct marking categories for each limb rather than just Arms/Legs
                HumanoidVisualLayers.RArm => MarkingCategories.RightArm,
                HumanoidVisualLayers.LArm => MarkingCategories.LeftArm,
                HumanoidVisualLayers.RHand => MarkingCategories.RightHand,
                HumanoidVisualLayers.LHand => MarkingCategories.LeftHand,
                HumanoidVisualLayers.LLeg => MarkingCategories.LeftLeg,
                HumanoidVisualLayers.RLeg => MarkingCategories.RightLeg,
                HumanoidVisualLayers.LFoot => MarkingCategories.LeftFoot,
                HumanoidVisualLayers.RFoot => MarkingCategories.RightFoot,
                // </Trauma>
                HumanoidVisualLayers.Tail => MarkingCategories.Tail,
                _ => MarkingCategories.Overlay
            };
        }
    }
}
