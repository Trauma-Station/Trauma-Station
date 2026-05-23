// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MobClass;

/// <summary>
/// This prototype groups together <see cref="MobClassPrototype"/>.
/// Useful for defining which classes a mob can specialize with.
/// </summary>
[Prototype]
public sealed partial class MobClassGroupPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The list of classes belonging to this group.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<MobClassPrototype>> Classes = new();

    /// <summary>
    ///  The path to the image which will be used as a background for the UI
    /// </summary>
    [DataField]
    public ResPath? BackgroundImagePath;

    /// <summary>
    ///  The scale of the background image.
    /// </summary>
    [DataField]
    public Vector2 BackgroundScale = new(2f, 2f);

    /// <summary>
    ///  The path to the image which will be used as texture for the specialize button for the UI
    /// </summary>
    [DataField]
    public ResPath? SpecializeButtonImagePath;

    /// <summary>
    ///  An optional patch to configure tiling stretching of the background. Used to set
    ///  the PatchMargin in a <code>StyleBoxTexture</code>
    /// </summary>
    [DataField]
    public Box2 BackgroundPatchMargin;

    /// <summary>
    /// Optional font override for descriptions of the classes.
    /// </summary>
    [DataField]
    public ResPath? FontPath;

    /// <summary>
    /// Optional font size override for <see cref="FontPath"/> font.
    /// </summary>
    [DataField]
    public int FontSize = 32;

    /// <summary>
    /// Overrides the color of the font.
    /// </summary>
    [DataField]
    public Color FontColorOverride  = Color.White;
}
