// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.MobClass;
using Robust.Client.ResourceManagement;

namespace Content.Trauma.Client.MobClass;

[GenerateTypedNameReferences]
public sealed partial class MobClassSelectorWindow : FancyWindow
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IResourceCache _resCache = default!;

    private SpriteSystem _sprite;

    /// <summary>
    /// Invoked when clicking <see cref="SpecializeTextureButton"/>.
    /// </summary>
    public Action<ProtoId<MobClassPrototype>?>? Specialize;

    /// <summary>
    /// The classes populated by the window.
    /// </summary>
    public List<EntProtoId>? Classes;

    /// <summary>
    /// The class we have selected, as a result of clicking one of the <see cref="Classes"/>.
    /// </summary>
    private ProtoId<MobClassPrototype>? _currentSelectedClass;

    public MobClassSelectorWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _sprite = _entMan.System<SpriteSystem>();
    }

    /// <summary>
    /// Handles populating the window with the parameters of <see cref="MobClassGroupPrototype"/>.
    /// If no UI parameters were set, it will use the default UI controls.
    /// </summary>
    public void PopulateWindow(ProtoId<MobClassGroupPrototype> group)
    {
        // Clear existing buttons
        DescriptionPanel.Visible = false;
        foreach (var child in ClassPanel.Children.ToList()) // linqgoida
        {
            if (child is not TextureButton button)
                continue;

            ClassPanel.RemoveChild(button);
        }

        if (!_proto.TryIndex(group, out var groupProto))
            return;

        ClassPanel.Columns = groupProto.Classes.Count;

        if (groupProto.BackgroundImagePath is { } backgroundImage)
        {
            var bgImg = _resCache.GetResource<TextureResource>(backgroundImage);
            var patch = groupProto.BackgroundPatchMargin;
            Background.PanelOverride = new StyleBoxTexture
            {
                Texture = bgImg,
                TextureScale =  groupProto.BackgroundScale,
                PatchMarginLeft = patch.Left,
                PatchMarginRight = patch.Right,
                PatchMarginTop = patch.Top,
                PatchMarginBottom = patch.Bottom,
            };
        }

        if (groupProto.SpecializeButtonImagePath is { } specializeButtonImagePath)
        {
            var specializeButton = _resCache.GetResource<TextureResource>(specializeButtonImagePath);
            SpecializeTextureButton.TexturePath = specializeButtonImagePath.CanonPath;
            SpecializeTextureButton.TextureNormal = specializeButton;
        }

        if (groupProto.FontPath is { } fontPath)
        {
            var font = _resCache.GetResource<FontResource>(fontPath);
            ClassDescription.FontColorOverride = groupProto.FontColorOverride;
            ClassDescription.FontOverride = new VectorFont(font, groupProto.FontSize);
        }

        foreach (var mobClass in groupProto.Classes)
        {
            if (!_proto.TryIndex(mobClass, out var mobClassProto))
                continue;

            var button = new TextureButton
            {
                TextureNormal = _sprite.Frame0(mobClassProto.Icon),
                ToolTip = mobClassProto.Name,
                Scale = new Vector2(4f, 4f),
            };

            button.OnPressed += _ => ButtonOnOnPressed(mobClassProto.Description, mobClassProto);

            ClassPanel.AddChild(button);
        }

        SpecializeTextureButton.OnPressed += _ => Specialize?.Invoke(_currentSelectedClass);
    }

    private void ButtonOnOnPressed(string desc, ProtoId<MobClassPrototype> mobClass)
    {
        DescriptionPanel.Visible = true;
        ClassDescription.Text = desc;
        _currentSelectedClass = mobClass;
    }
}
