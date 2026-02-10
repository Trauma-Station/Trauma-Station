using System.Linq;
using System.Numerics;
using Content.Shared.Humanoid.Markings;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Humanoid;

public sealed partial class MarkingPicker
{
    private bool TryCreateShaderParamSliders(MarkingPrototype prototype, int i, BoxContainer colorContainer)
    {
        if (_markingManager.GetMarkingShaderParams?.Invoke(prototype, i, _currentMarkings) is not { } shaderParams)
            return false;

        List<Slider> sliders = new();
        var paramIndex = _currentMarkingColors.Count;

        foreach (var (name, param) in shaderParams.OrderBy(x => x.Key))
        {
            var box = new BoxContainer { HorizontalExpand = true };

            var slider = new Slider
            {
                HorizontalExpand = true,
                MinValue = param.X,
                MaxValue = param.Y,
                Value = param.Z
            };

            var spinBox = new FloatSpinBox(0.01f, 2) { Value = param.Z };

            sliders.Add(slider);

            spinBox.IsValid += val => val >= param.X && val <= param.Y;

            slider.OnValueChanged += range =>
            {
                // OnValueChanged doesn't get triggered
                spinBox.Value = range.Value;
                SliderChanged();
            };
            spinBox.OnValueChanged += args =>
            {
                // OnValueChanged gets triggered
                slider.Value = args.Value;
            };

            box.AddChild(slider);
            box.AddChild(spinBox);
            colorContainer.AddChild(new Label { Text = $"{name}:" });
            colorContainer.AddChild(box);
        }

        _currentMarkingColors.Add(GetParamColor());

        return true;

        void SliderChanged()
        {
            _currentMarkingColors[paramIndex] = GetParamColor();

            ColorChanged(paramIndex);
        }

        Color GetParamColor()
        {
            var vec = Vector4.Zero;

            for (var j = 0; j < sliders.Count && j < 4; j++)
            {
                vec[j] = sliders[j].Value;
            }

            return new Color(vec);
        }
    }

    private Texture? GetMarkingTexture(MarkingPrototype marking)
    {
        if (marking.Sprites.Count > 0)
            return _sprite.Frame0(marking.Sprites[0]);

        var markings = _currentMarkings.Markings.ToDictionary();
        if (HairMarking != null)
            markings[MarkingCategories.Hair] = new() { HairMarking };
        if (FacialHairMarking != null)
            markings[MarkingCategories.FacialHair] = new() { FacialHairMarking };

        var otherMarking = markings
            .GetValueOrDefault(MarkingCategoriesConversion.FromHumanoidVisualLayers(marking.BodyPart))
            ?.FirstOrDefault();

        if (otherMarking == null || !_markingManager.TryGetMarking(otherMarking, out var otherMarkingProto))
            return null;

        var sprite = otherMarkingProto.Sprites.FirstOrDefault();

        return sprite == null ? null : _sprite.Frame0(sprite);
    }
}
