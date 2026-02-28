// <Trauma>
using Robust.Shared.Timing;
// </Trauma>
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Humanoid;

public sealed class EyeColorPicker : Control
{
    // <Trauma>
    [Dependency] private readonly IGameTiming _timing = default!;
    private uint _lastColorUpdate;
    // </Trauma>

    public event Action<Color>? OnEyeColorPicked;

    private readonly ColorSelectorSliders _colorSelectors;

    private Color _lastColor;

    public void SetData(Color color)
    {
        _lastColor = color;

        _colorSelectors.Color = color;
    }

    public EyeColorPicker()
    {
        IoCManager.InjectDependencies(this); // Trauma
        var vBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        AddChild(vBox);

        vBox.AddChild(_colorSelectors = new ColorSelectorSliders());
        _colorSelectors.SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv; // defaults color selector to HSV

        _colorSelectors.OnColorChanged += ColorValueChanged;
    }

    private void ColorValueChanged(Color newColor)
    {
        // <Trauma> - dont lag the shit out of the game
        var now = _lastColorUpdate;
        if (newColor == _lastColor || _lastColorUpdate == now)
            return;

        _lastColorUpdate = now;
        // </Trauma>
        OnEyeColorPicked?.Invoke(newColor);

        _lastColor = newColor;
    }
}
