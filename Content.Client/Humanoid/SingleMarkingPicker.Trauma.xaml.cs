using System.Linq;
using Content.Shared.Humanoid.Markings;
using Robust.Client.Graphics;

namespace Content.Client.Humanoid;

public sealed partial class SingleMarkingPicker
{
    public Texture? GetMarkingTexture(MarkingPrototype marking)
    {
        if (marking.Sprites.Count > 0)
            return _sprite.Frame0(marking.Sprites[0]);

        var category =  MarkingCategoriesConversion.FromHumanoidVisualLayers(marking.BodyPart);

        var otherMarking = _markingPrototypeCache?.Values.FirstOrDefault(x => x.MarkingCategory == category);

        if (otherMarking == null)
            return null;

        var sprite = otherMarking.Sprites.FirstOrDefault();

        return sprite == null ? null : _sprite.Frame0(sprite);
    }
}
