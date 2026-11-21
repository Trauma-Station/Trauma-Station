namespace Content.Client.UserInterface.Controls;

public sealed partial class SimpleRadialMenu
{
    public bool CloseOnPressed = true;
}

public abstract partial class RadialMenuOptionBase
{
    /// <summary>
    /// Color for button border.
    /// Is used only with sector radial (<see cref="SimpleRadialMenuSettings.UseSectors"/>).
    /// </summary>
    public Color? BorderColor { get; set; }

    /// <summary>
    /// Color for button border when it is hovered.
    /// Is used only with sector radial (<see cref="SimpleRadialMenuSettings.UseSectors"/>).
    /// </summary>
    public Color? HoverBorderColor { get; set; }

    /// <summary>
    /// Color modulation for the icon.
    /// Does nothing if <see cref="IconSpecifier"/> is null.
    /// </summary>
    public Color Color = Color.White;
}
