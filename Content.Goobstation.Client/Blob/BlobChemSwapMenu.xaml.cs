// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Goobstation.Client.Blob;

[GenerateTypedNameReferences]
public sealed partial class BlobChemSwapMenu : DefaultWindow
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IEntityManager _ent = default!;
    private readonly SpriteSystem _sprite;

    public event Action<ProtoId<BlobChemPrototype>>? OnSetChem;

    private EntityUid _owner;
    private ProtoId<BlobChemPrototype> _selected;
    private Dictionary<ProtoId<BlobChemPrototype>, Button> _buttons = new();

    private static readonly EntProtoId PreviewTile = "NormalBlobTile";

    public BlobChemSwapMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _sprite = _ent.System<SpriteSystem>();
        PopulateGrid();
    }

    public void SetOwner(EntityUid owner)
    {
        _owner = owner;
        Update();
    }

    private void Update()
    {
        if (!_ent.TryGetComponent<BlobCoreComponent>(_owner, out var core))
            return;

        var selected = core.CurrentChem;
        if (selected == _selected)
            return;

        _selected = selected;
        foreach (var (id, button) in _buttons)
        {
            button.Pressed = selected == id;
        }
    }

    private void PopulateGrid()
    {
        var group = new ButtonGroup();

        var proto = _proto.Index(PreviewTile);
        var texture = _sprite.GetPrototypeIcon(proto);
        foreach (var chem in _proto.EnumeratePrototypes<BlobChemPrototype>())
        {
            var id = chem.ID;
            var button = new Button
            {
                MinSize = new Vector2(64, 64),
                HorizontalExpand = true,
                Group = group,
                StyleClasses = {StyleClass.ButtonSquare},
                ToggleMode = true,
                Pressed = _selected == id,
                ToolTip = $"{chem.Name}\n{chem.Info}",
                TooltipDelay = 0.01f,
            };
            button.OnPressed += _ => OnSetChem?.Invoke(id);

            button.AddChild(new TextureRect
            {
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                Modulate = chem.Color,
                Texture = texture.Default,
            });

            _buttons[id] = button;
            Grid.AddChild(button);
        }
    }
}
